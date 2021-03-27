const int buttonPin = 2;
const int ledPin    = 12;

const int STATE_NOCONN    = -1;
const int STATE_READY     = 0;
const int STATE_STARTING  = 1;
const int STATE_RUNNING   = 2;
const int STATE_DONE      = 3;
const int STATE_RESET     = 4;

int    playState    = STATE_NOCONN;
int    playHead     = 0;
int    heartBeat;
int    prevHeart;
int    buttonState;
int    prevButton;
long   blinkMillis;
long   playMillis;
long   heartMillis;
long   resetMillis;
int    value;

int    blink_onCount = 0;
int    blink_offCount = 0;
int    blink_state = 0;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  pinMode(ledPin, OUTPUT);
  pinMode(buttonPin, INPUT);
  playState = STATE_NOCONN;
}

void loop() {
  // put your main code here, to run repeatedly:
  if (heartBeat != prevHeart) {
    heartMillis = millis();
    prevHeart = heartBeat;
  } else if (millis() - heartMillis > 10000) {
    playState = STATE_NOCONN;
  }
  if (playState == STATE_NOCONN) {
    blink(500, 5, 3);
    playHead = 0;
    readPlayHead();
    if (heartBeat == HIGH) {
      playState = STATE_READY;
      heartMillis = millis();
    }
  }
  buttonState = digitalRead(buttonPin);
  if (buttonState != prevButton) {
    resetMillis = millis();
    prevButton = buttonState;
  }
  if (buttonState == HIGH && millis() - resetMillis > 5000) {
    playState = STATE_RESET;
  }
  if (playState == STATE_READY) {
    blink(500, 1, 5);
    if (buttonState == HIGH) {
      digitalWrite(ledPin, HIGH);
      Serial.println("PLAY");
      Serial.flush();
      playState = STATE_STARTING;
      playMillis = millis();
    } else {
      readPlayHead();
    }
  }
  if (playState == STATE_STARTING) {
    blink(100);
    readPlayHead();
    if (playHead > 0) {
      playState = STATE_RUNNING;
    } else if (playHead < 0) {
      playState = STATE_RESET;
      playMillis = millis();
    } else if (millis() - playMillis > 5000) {
      playState = STATE_READY;
    }
  }
  if (playState == STATE_RUNNING) {
    digitalWrite(ledPin, HIGH);
    readPlayHead();
    if (playHead < 0) {
      playState = STATE_RESET;
      playMillis = millis();
    } else if (playHead >= 100) {
      playState = STATE_DONE;
      playMillis = millis();
    } else {
      /* don't worry, be happy */
    }
  }
  if (playState == STATE_DONE) {
    blink(100, 5, 2);
    if (millis() - playMillis > 1500) {
      playState = STATE_READY;
    }
  }
  if (playState == STATE_RESET) {
    blink(500, 2, 3);
    if (millis() - playMillis > 2000) {
      playState = STATE_NOCONN;
    }
  }
}

void blink(long rate)
{
  if (millis() - blinkMillis > rate) {
    blinkMillis = millis();
    if (blink_state == HIGH) {
      digitalWrite(ledPin, LOW);
      blink_state = LOW;
    } else {
      digitalWrite(ledPin, HIGH);
      blink_state = HIGH;
    }
  }
}
void blink(long interval, int ons, int offs)
{
  if (millis() - blinkMillis > interval) {
    blinkMillis = millis();
    if (blink_onCount > 0) {
      if (blink_state == HIGH) {
        digitalWrite(ledPin, LOW);
        blink_state = LOW;
        blink_onCount--;
      } else {
        digitalWrite(ledPin, HIGH);
        blink_state = HIGH;
      }
      blink_offCount = offs;
    } else if (blink_offCount > 0) {
      if (blink_state == HIGH) {
        digitalWrite(ledPin, LOW);
        blink_state = LOW;
        blink_offCount--;
      } else {
        digitalWrite(ledPin, LOW);
        blink_state = HIGH;
      }
      if (blink_offCount == 0) {
        blink_onCount = ons;
      }
    } else {
      blink_state = LOW;
      blink_onCount = ons;
      blink_offCount = offs;
    }
  }
}

void readPlayHead() {
  if (Serial.available()) {
    char ch = Serial.read();
    if (isDigit(ch)) {
      value = value * 10 + (ch - '0');
    } else if (ch == 'H') {
      heartBeat = HIGH;
    } else {
      playHead = value;
      value = 0;
    }
  } else {
    heartBeat = LOW;
  }
}
