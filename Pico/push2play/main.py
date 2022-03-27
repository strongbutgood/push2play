'''
Main code to run automatically on power up of pico.

Modify this according to the application that is required.
It is recommended that the internal led be used to indicate activity.
'''
import sys, select
from machine import Pin, Timer

try:
    import utime
except ImportError:
    import time as utime

ledPin = Pin(25, Pin.OUT)
timer = Timer()

def toggleLed(timer):
    ledPin.toggle()

# flash led on/off twice a second
timer.init(freq=2.0, mode=Timer.PERIODIC, callback=toggleLed)
# do this for five seconds
utime.sleep(5.0)

from serial import USB
b2 = Pin(12, Pin.IN, Pin.PULL_UP)
b3 = Pin(19, Pin.IN, Pin.PULL_UP)

def usb_task():
    # loop forever
    timeout_ms = utime.ticks_ms()
    play_ms = utime.ticks_ms()
    heartbeat_ms = utime.ticks_ms()
    starting = False
    playing = False
    connected = False
    while True:
        now = utime.ticks_ms()
        # print if heartbeat time done
        if utime.ticks_diff(now, heartbeat_ms) > 0:
            heartbeat_ms = utime.ticks_add(now, 1000)
            USB.write(".\n" if connected else "!\n")
        # read USB serial port
        line = USB.read_line()
        if line is None or len(line) == 0:
            if utime.ticks_diff(now, timeout_ms) > 0:
                # flash led on/off ten times a second
                timer.init(freq=10.0, mode=Timer.PERIODIC, callback=toggleLed)
                connected = False
        elif line.strip().upper() == 'H':
            if not playing:
                # heartbeat received in idle, flash led on/off once a second
                timer.init(freq=1.0, mode=Timer.PERIODIC, callback=toggleLed)
            # update the heartbeat timeout by 3 seconds
            timeout_ms = utime.ticks_add(now, 3000)
            connected = True
        else:
            # if line is an integer
            try:
                val = int(line)
                if val >= 100:
                    # led off and prevent new play request by 5 seconds
                    play_ms = utime.ticks_add(now, 5000)
                    timer.init(freq=1.0, mode=Timer.PERIODIC, callback=toggleLed)
                    timeout_ms = utime.ticks_add(now, 3000)
                    playing = False
                    starting = False
                    ledPin.off()
                    USB.write("END\n")
                elif val > 0:
                    # led on solid until 100 is received
                    timer.deinit()
                    ledPin.on()
                    playing = True
                    starting = False
                    USB.write("START\n")
            except ValueError:
                pass
        # if either button pressed, send PLAY
        if connected and not starting and not playing and utime.ticks_diff(now, play_ms) > 0:
            if not b2.value() or not b3.value():
                starting = True
                USB.write("PLAY\n")
        if starting and b2.value() and b3.value():
            starting = False
        utime.sleep_ms(100)

while True:
    USB.start(echo=False)
    USB.with_keyboard_interrupt(usb_task)

# print is readable from USB as serial 115.2k baud
print("Hello from PICO!")

print("And Goodbye!")

#end of program
timer.deinit()
ledPin.off()
