import network
import machine
import os
import ujson
from machine import Pin, ADC, PWM
import utime
import socket
import ure
import random
import select
import sys
import uselect

# 硬件配置 ====================================================
MOTOR_DIR = Pin(0, Pin.OUT)
MOTOR_PUL = Pin(1, Pin.OUT)
PWM_PUL = PWM(MOTOR_PUL)
PWM_PUL.duty_u16(0)  # 50% duty cycle
PWM_PUL.freq(1000) 
LED_POWER = Pin(2, Pin.OUT)
LED_WIFI = Pin(3, Pin.OUT)
LED_BOARD = Pin("LED", Pin.OUT)

POS_SENSOR1 = ADC(Pin(26))
POS_SENSOR2 = ADC(Pin(27))

SWITCH_PIN = Pin(4, Pin.IN, Pin.PULL_DOWN)

# 运动参数 =====================================================
MICROSTEP = 1
DISTANCE_PER_REV = 0.4 * 1.2  # mm, 1.2 is determined by the actual measurement
STEPS_PER_REV = 200
MAX_SPEED = 500  # mm/min
SYRINGE_PROFILES = {
    0 : 0, 1 : 4.74, 5 : 12.6, 10 : 15.17, 30 : 23.6  # 注射器直径(mm)
}
SYRINGE = 30 # default syringe size
DIRECTION_PULL = 0
DIRECTION_PUSH = 1

# 全局状态 =====================================================
class State:
    IDLE = 0
    MOVING = 1
    STOPPING = 2

motor_state = State.IDLE
frequency = 0
motion_time = 0 # motion time in seconds
start_time = utime.ticks_ms()
direction = DIRECTION_PUSH
emergency_stop = False
switch_pressed_time = 0

# WiFi配置 =====================================================
WIFI_SSID = ''
WIFI_PWD = ''
HOST_NAME = 'PumpAP' + str(random.randint(0,100))
AP_CONFIG = (HOST_NAME, 'bsjinstrument')
CONFIG_FILE = 'wifi.json'

# 电机控制函数 =================================================
def update_motor():
    global motor_state, current_steps, last_step, emergency_stop
    if motor_state != State.MOVING:
        return
    # 碰撞检测
    if (direction == DIRECTION_PUSH and read_sensor(POS_SENSOR1)) or \
       (direction == DIRECTION_PULL and read_sensor(POS_SENSOR2)):
        emergency_stop = True
        print('Motion stopped because obstacle met')
    
    # 紧急停止或完成运动
    if emergency_stop:
        motor_state = State.STOPPING
        PWM_PUL.duty_u16(0)
        motor_state = State.IDLE
        print('Motion stopped because stop signal received')
        emergency_stop = False
        return
    
    # 步进脉冲生成
    now = utime.ticks_ms()
    if utime.ticks_diff(now, start_time) > motion_time * 1000:
        motor_state = State.STOPPING
        PWM_PUL.duty_u16(0)
        motor_state = State.IDLE
        print('Motion stopped because target reached')
        return


def read_sensor(sensor):
    return sensor.read_u16() * 3.3 / 65535 < 0.5 # Return True when sensor value is low, meaning obstacle detected

def calculate_motion(syringe, volume, speed):
    diameter = SYRINGE_PROFILES.get(syringe, 20)
    if syringe == 0:
        distance = volume
    else:
        area = 3.14159 * (diameter/2)**2  # mm²
        distance = (volume * 1000) / area  # mm 
    speed = distance / (volume / speed) / 60 # mm/s
    time = distance / speed  # s
    print(f'distance: {distance} mm, speed: {speed} mm/s, time: {time}')
    frequency = int(speed / DISTANCE_PER_REV * STEPS_PER_REV * MICROSTEP)  # Calculate frequency for PWM
    return frequency, time

def reset_motor():
    global motor_state, direction, frequency, motion_time, PWM_PUL, start_time
    motor_state = State.MOVING
    direction = DIRECTION_PUSH
    MOTOR_DIR.value(direction)
    motion_time = 20
    start_time = utime.ticks_ms()
    PWM_PUL.freq(2000)
    PWM_PUL.duty_u16(32768)

# 网络处理函数 =================================================
class WebServer:
    def __init__(self, ip):
        self.sock = socket.socket()
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.sock.bind(('0.0.0.0', 80))
        self.sock.listen(5)
        #self.sock.settimeout(0.1)
        self.sock.setblocking(False)
        self.routes = {
            r'GET /': self.handle_root,
            r'POST /move': self.handle_move,
            r'POST /stop': self.handle_stop,
            r'POST /reset': self.handle_reset,
            r'POST /configure': self.handle_configure
        }
        self.poll = select.poll()
        self.poll.register(self.sock, select.POLLIN)  # 使用poll机制
    
    def process(self):
        res = self.poll.poll(50)  # 50ms超时
        for sock, event in res:
            if sock == self.sock:
                try:
                    cl, addr = self.sock.accept()
                    cl.setblocking(False)
                    self.poll.register(cl, select.POLLIN)
                except OSError:
                    pass
            else:
                self.handle_client(sock)

    def handle_client(self, cl):
        try:
            request = cl.recv(1024).decode()
            if request:  # 确保收到有效请求
                self.handle_request(cl, request)
        except Exception as e:
            print('Client error:', e)
        finally:
            self.poll.unregister(cl)
            cl.close()
    
    def handle_request(self, cl, request):
        for pattern, handler in self.routes.items():
            if ure.match(pattern, request):
                return handler(cl, request)
        self.send_response(cl, "Not Found", status=404)
    
    def handle_root(self, cl, request):
        options = ''.join(f'<option value="{k}">{k}mL</option>' for k in sorted(SYRINGE_PROFILES))
        form = f"""
        <div class="card">
        <h2>WIFI Control</h2>
        <form action="/configure" method="post" enctype="application/x-www-form-urlencoded">
            <label for="ssid">WiFi Name:</label><br>
            <input type="text" id="ssid" name="ssid" value={WIFI_SSID}><br>
            <label for="password">WiFi passcode:</label><br>
            <input type="text" id="password" name="password" value={WIFI_PWD}><br>
            <label for="name">Pump name:</label><br>
            <input type="text" id="name" name="name" value={HOST_NAME}><br>
            <br>
            <button type="submit">Configure</button>
        </form>
          <h2>Motion</h2>
          <form action="/move" method="post" target="hidden-form">
            <select name="dir"><option value="1">Out</option><option value="0">In</option></select>
            <input type="number" name="vol" placeholder="Volume (mL)" step="0.01" required>
            <input type="number" name="speed" placeholder="Speed (mL/min)" min="0"  step="0.01" max={MAX_SPEED} required>
            <select name="syr">{options}</select>
            <button type="submit">Start</button>
          </form>
        </div>
        <div class="card">
          <form action="/stop" method="post" target="hidden-form"><button>Stop</button></form><br />
          <form action="/reset" method="post" target="hidden-form"><button type="button" onclick="this.form.submit()">Reset</button></form>
        </div>
        """
        self.send_response(cl, """<!DOCTYPE html>
<html><head>
<title>Pump Control</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
  body{max-width:400px;margin:20px auto;padding:20px;font-family:Arial,sans-serif}
  .card{padding:20px;border-radius:10px;box-shadow:0 2px 5px rgba(0,0,0,0.1);margin-bottom:20px}
  input,select{width:100%;padding:8px;margin:5px 0 15px;border:1px solid #ddd;border-radius:4px}
  button{background:#007bff;color:white;border:none;padding:10px 20px;border-radius:4px;cursor:pointer}
  button:disabled{background:#6c757d}
</style>
</head><body><iframe style="display:none" name="hidden-form"></iframe>
""" + form + "</body></html>")
    
    def handle_move(self, cl, request):
        global motor_state, direction, frequency, motion_time, PWM_PUL, start_time, SYRINGE
        params = self.parse_params(request)
        try:
            SYRINGE = int(params.get('syr',20))
            volume = float(params.get('vol',0))
            speed = min(MAX_SPEED, float(params.get('speed',1)))
            direction = int(params.get('dir',0))
            print(f"Move: {direction}, {volume}mL, {speed}mL/min, {SYRINGE}mL syringe")
            frequency, motion_time = calculate_motion(SYRINGE, volume, speed)
            motor_state = State.MOVING
            MOTOR_DIR.value(direction)
            start_time = utime.ticks_ms()
            PWM_PUL.freq(frequency)
            PWM_PUL.duty_u16(32768)
            print(f'Moving: {frequency}Hz, {motion_time}s')
            self.send_response(cl, "Motion start")
        except Exception as e:
            self.send_response(cl, f"Parameter error: {str(e)}", status=400)
    
    def handle_configure(self, cl, request):
        global WIFI_SSID, WIFI_PWD, HOST_NAME
        params = self.parse_params(request)
        try:
            WIFI_SSID = params.get('ssid', '')
            WIFI_PWD = params.get('password', '')
            HOST_NAME = params.get('name', '')
            with open(CONFIG_FILE, 'w') as f:
                ujson.dump({'ssid': WIFI_SSID, 'password': WIFI_PWD, 'name': HOST_NAME}, f)
            ipaddress = connect_wifi(WIFI_SSID, WIFI_PWD)
            print(ipaddress)
            if ipaddress != False:
                self.send_response(cl, f"""
WiFi configured! Now you can connect to the pump at: {ipaddress} within the WiFi you set. <br>
Now the AP mode is to be disabled.
                               """)
                utime.sleep(1)
                disable_ap_mode()
            else:
                raise Exception('WiFi connection failed')
        except Exception as e:
            self.send_response(cl, f"Parameter error: {str(e)}", status=400)

    def handle_stop(self, cl, request):
        global emergency_stop
        print('Stop signal received')
        emergency_stop = True
        self.send_response(cl, "Stopped")
    
    def handle_reset(self, cl, request):
        reset_motor()
        self.send_response(cl, "Reset")
    
    def parse_params(self, request):
        params = {}
        if '\r\n\r\n' in request:
            body = request.split('\r\n\r\n')[1]
            pairs = body.split('&')
            for pair in pairs:
                if '=' in pair:
                    key, value = pair.split('=')
                    params[key] = value
        return params
    
    def send_response(self, cl, msg, status=200):
        cl.send(f"HTTP/1.1 {status} OK\r\nContent-Type: text/html\r\n\r\n{msg}")

# Function to delete all .json files
def reset_wifi():
    for file in os.listdir():
        if file.endswith(".json"):
            os.remove(file)
    print("Wifi information cleared.")

import re
def handle_serial_data(data):
    global motor_state, direction, frequency, motion_time, PWM_PUL, start_time, SYRINGE, emergency_stop
    # Remove any whitespace, newlines, or carriage returns
    data = data.decode().strip().lower()
    # Check for 'set syringe' command
    match = re.match(r'set syringe (\d+)', data)
    
    if match:
        SYRINGE = int(match.group(1))
        return
    # Check for 'move direction volume speed' command
    match = re.match(r'move (in|out) (\d+(\.\d+)?) (\d+(\.\d+)?)', data)
    if match:
        direction = match.group(1)
        volume = float(match.group(2))
        speed = float(match.group(4))
        frequency, motion_time = calculate_motion(SYRINGE, volume, speed)
        motor_state = State.MOVING
        MOTOR_DIR.value(DIRECTION_PULL if direction == 'in' else DIRECTION_PUSH)
        start_time = utime.ticks_ms()
        PWM_PUL.freq(frequency)
        PWM_PUL.duty_u16(32768)
        print(f'Moving: {frequency}Hz, {motion_time}s')
        return
    if data == 'stop':
        emergency_stop = True
        return
    if data == 'reset':
        reset_motor()
        return

def check_switch():
    global switch_pressed_time
    if SWITCH_PIN.value() == 1:
        if switch_pressed_time == 0:
            switch_pressed_time = utime.ticks_ms()
        elif utime.ticks_diff(utime.ticks_ms(), switch_pressed_time) > 3000:
            reset_wifi()
            machine.reset()
    else:
        if switch_pressed_time != 0 and utime.ticks_diff(utime.ticks_ms(), switch_pressed_time) < 3000:
            machine.reset()
        switch_pressed_time = 0

# 主程序 =======================================================
def main():
    LED_POWER.on()
    LED_WIFI.off()
    global HOST_NAME, WIFI_SSID, WIFI_PWD, AP_CONFIG, switch_pressed_time
    switch_pressed_time = 0
    # WiFi连接管理
    if CONFIG_FILE in os.listdir():
        with open(CONFIG_FILE) as f:
            wifi = ujson.load(f)
            WIFI_SSID = wifi.get('ssid', '')
            WIFI_PWD = wifi.get('password', '')
            HOST_NAME = wifi.get('name', '')
            disable_ap_mode()
            if connect_wifi(WIFI_SSID, WIFI_PWD):
                print(f'WiFi connected: {WIFI_SSID}')
    
    if not network.WLAN().isconnected():
        print('WiFi not connected. Starting AP mode...')
        start_ap_mode()
    
    server = WebServer(network.WLAN().ifconfig()[0])
    
    # 设置串口监听
    uart = machine.UART(0, baudrate=9600, tx=Pin(12), rx=Pin(13)) 
    # 主循环
    while True:
        server.process()
        update_motor()
        check_switch()
        if uart.any():
            data = uart.read()
            handle_serial_data(data)
        utime.sleep_ms(1)

def connect_wifi(ssid, pwd, timeout=10):
    wlan = network.WLAN(network.STA_IF)
    wlan.active(True)
    wlan.connect(ssid, pwd)
    for _ in range(timeout):
        if wlan.isconnected():
            print(wlan.ifconfig())
            LED_WIFI.on()
            return wlan.ifconfig()[0] # return IP address if connected
        utime.sleep(1)
    return False


def start_ap_mode():
    ap = network.WLAN(network.AP_IF)
    ap.config(essid=AP_CONFIG[0], password=AP_CONFIG[1])
    ap.active(True)
    while not ap.active():
        pass
    print('AP mode:', ap.ifconfig())

def disable_ap_mode():
    ap = network.WLAN(network.AP_IF)
    ap.active(False)

if __name__ == '__main__':
    main()