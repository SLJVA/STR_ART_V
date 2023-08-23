// Odczytywanie z karty micro SD
#include "FS.h"
#include "SD_MMC.h"
#define ONE_BIT_MODE true
#include <WiFi.h>
#include <WiFiClient.h>
#include <WebServer.h>
#include <ESPmDNS.h>
#include <Update.h>
#include <WebSocketsServer.h>

const char* host = "esp32";
const char* ssid = "DELL_Network";
const char* password = "***";


/*
 * Login page
 */
const char* loginIndex = 
 "<form name='loginForm'>"
    "<table width='20%' bgcolor='A09F9F' align='center'>"
        "<tr>"
            "<td colspan=2>"
                "<center><font size=4><b>ESP32 Login Page</b></font></center>"
                "<br>"
            "</td>"
            "<br>"
            "<br>"
        "</tr>"
        "<td>Username:</td>"
        "<td><input type='text' size=25 name='userid'><br></td>"
        "</tr>"
        "<br>"
        "<br>"
        "<tr>"
            "<td>Password:</td>"
            "<td><input type='Password' size=25 name='pwd'><br></td>"
            "<br>"
            "<br>"
        "</tr>"
        "<tr>"
            "<td><input type='submit' onclick='check(this.form)' value='Login'></td>"
        "</tr>"
    "</table>"
"</form>"
"<script>"
    "function check(form)"
    "{"
    "if(form.userid.value=='admin' && form.pwd.value=='admin')"
    "{"
    "window.open('/serverIndex')"
    "}"
    "else"
    "{"
    " alert('Error Password or Username')/*displays error message*/"
    "}"
    "}"
"</script>";
 
/*
 * Server Index Page
 */
 
const char* serverIndex = 
"<script src='https://ajax.googleapis.com/ajax/libs/jquery/3.2.1/jquery.min.js'></script>"
"<form method='POST' action='#' enctype='multipart/form-data' id='upload_form'>"
   "<input type='file' name='update'>"
        "<input type='submit' value='Update'>"
    "</form>"
 "<div id='prg'>progress: 0%</div>"
 "<script>"
  "$('form').submit(function(e){"
  "e.preventDefault();"
  "var form = $('#upload_form')[0];"
  "var data = new FormData(form);"
  " $.ajax({"
  "url: '/update',"
  "type: 'POST',"
  "data: data,"
  "contentType: false,"
  "processData:false,"
  "xhr: function() {"
  "var xhr = new window.XMLHttpRequest();"
  "xhr.upload.addEventListener('progress', function(evt) {"
  "if (evt.lengthComputable) {"
  "var per = evt.loaded / evt.total;"
  "$('#prg').html('progress: ' + Math.round(per*100) + '%');"
  "}"
  "}, false);"
  "return xhr;"
  "},"
  "success:function(d, s) {"
  "console.log('success!')" 
 "},"
 "error: function (a, b, c) {"
 "}"
 "});"
 "});"
 "</script>";


WebSocketsServer webSocket = WebSocketsServer(8080); // Numer portu WebSocket
IPAddress staticIP(192, 168, 5, 106);  // Stały adres IP
IPAddress gateway(192, 168, 5, 1);
IPAddress subnet(255, 255, 255, 0);

WebServer server(80); //Nr portu - Programming – Web Updater

char receivedPayload[64];

// Definiowanie pinów dla silników krokowych
const int X1_STEP_PIN = 12;
const int X1_DIR_PIN = 14;
const int X2_STEP_PIN = 26;
const int X2_DIR_PIN = 15;
const int Y_STEP_PIN = 33;
const int Y_DIR_PIN = 27;
const int Z_STEP_PIN = 25;
const int Z_DIR_PIN = 32;

const int RELAY = 13;

// Definiowanie prędkości silników
int X_SPEED = 500;
int Y_SPEED = 500;
int Z_SPEED = 500;
const int BASE_SPEED = 650;
int accel_step_x = 0;
int dVx = 0;
int accel_step_y = 0;
int dVy = 0;
int accel_step_z = 0;
int dVz = 0;

// Definiowanie kierunku ruchu silników
const int X1_FORWARD = LOW;
const int X1_BACKWARD = HIGH;
const int X2_FORWARD = LOW;
const int X2_BACKWARD = HIGH;
const int Y_FORWARD = LOW;
const int Y_BACKWARD = HIGH;
const int Z_FORWARD = LOW;
const int Z_BACKWARD = HIGH;

// Definiowanie wartości dla jednego obrotu silnika
const int STEPS_PER_REV = 200;
const float DISTANCE_PER_REV = 8.0;

void parseGcode(String inputString, float &x, float &y, float &z, float &f) {
  static float last_x = 0.0; // zmienna statyczna przechowująca ostatnią wartość osi x
  static float last_y = 0.0; // zmienna statyczna przechowująca ostatnią wartość osi y
  static float last_z = 0.0; // zmienna statyczna przechowująca ostatnią wartość osi z
  static float last_f = 0.0; // zmienna statyczna przechowująca ostatnią wartość prędkości ruchu


  if (inputString.startsWith("G1")) { // Sprawdź, czy linia rozpoczyna się od "G1"
    int xIndex = inputString.indexOf('X'); // Znajdź indeks znaku 'X'
    int yIndex = inputString.indexOf('Y'); // Znajdź indeks znaku 'Y'
    int zIndex = inputString.indexOf('Z'); // Znajdź indeks znaku 'Z'
    int fIndex = inputString.indexOf('F'); // Znajdź indeks znaku 'F'

    if (xIndex != -1) { // Jeśli znaleziono 'X' w linii
      x = inputString.substring(xIndex + 1, yIndex != -1 ? yIndex : zIndex != -1 ? zIndex : fIndex != -1 ? fIndex : inputString.length()).toFloat();
      last_x = x; // Zapisz wartość do zmiennej statycznej
    } else {
      x = last_x; // Użyj poprzedniej wartości, jeśli 'X' nie jest podane
    }

    if (yIndex != -1) { // Jeśli znaleziono 'Y' w linii
      y = inputString.substring(yIndex + 1, zIndex != -1 ? zIndex : fIndex != -1 ? fIndex : inputString.length()).toFloat();
      last_y = y; // Zapisz wartość do zmiennej statycznej
    } else {
      y = last_y; // Użyj poprzedniej wartości, jeśli 'Y' nie jest podane
    }

    if (zIndex != -1) { // Jeśli znaleziono 'Z' w linii
      z = inputString.substring(zIndex + 1, fIndex != -1 ? fIndex : inputString.length()).toFloat();
      last_z = z; // Zapisz wartość do zmiennej statycznej
    } else {
      z = last_z; // Użyj poprzedniej wartości, jeśli 'Z' nie jest podane
    }

    if (fIndex != -1) { // Jeśli znaleziono 'F' w linii
     if (inputString.substring(fIndex + 1).toFloat() > 0) { // Sprawdź, czy wartość 'F' jest większa od zera
        f = inputString.substring(fIndex + 1).toFloat();
        last_f = f; // Zapisz wartość do zmiennej statycznej
      } else {
        f = last_f; // Użyj poprzedniej wartości, jeśli wartość 'F' jest niepoprawna
      }
    } else {
      f = last_f; // Użyj poprzedniej wartości, jeśli 'F' nie jest podane
    }
  } else {
    // Błąd: nie rozpoznano komendy 'G1'
    x = last_x;
    y = last_y;
    z = last_z;
    f = last_f;
    Serial.println("Error: unrecognized command");
  }
} 
    

// Funkcja do ruchu do określonej pozycji
void move(float x, float y, float z, float f) {
    int x_steps = int(x / DISTANCE_PER_REV * STEPS_PER_REV);
    int y_steps = int(y / DISTANCE_PER_REV * STEPS_PER_REV);
    int z_steps = int(z / DISTANCE_PER_REV * STEPS_PER_REV);
    
    int SPEED = 650 - (4 * int(f));

    digitalWrite(X1_DIR_PIN, x_steps > 0 ? X1_FORWARD : X1_BACKWARD);
    digitalWrite(X2_DIR_PIN, x_steps > 0 ? X2_FORWARD : X2_BACKWARD);
    x_steps = abs(x_steps);
    
    digitalWrite(Y_DIR_PIN, y_steps > 0 ? Y_FORWARD : Y_BACKWARD);
    y_steps = abs(y_steps);
    
    digitalWrite(Z_DIR_PIN, z_steps > 0 ? Z_FORWARD : Z_BACKWARD);
    z_steps = abs(z_steps);

    
    int X_CURR_SPEED = BASE_SPEED;
    int Y_CURR_SPEED = BASE_SPEED;
    int Z_CURR_SPEED = BASE_SPEED;
    int CURR_SPEED = 0;

    int accel_step_x = (x_steps > 400) ? 200 : x_steps / 2;
    int accel_step_y = (y_steps > 400) ? 200 : y_steps / 2;
    int accel_step_z = (z_steps > 400) ? 200 : z_steps / 2;

    int dVx = (accel_step_x > 0) ? (BASE_SPEED - SPEED) / accel_step_x : 0;
    int dVy = (accel_step_y > 0) ? (BASE_SPEED - SPEED) / accel_step_y : 0;
    int dVz = (accel_step_z > 0) ? (BASE_SPEED - SPEED) / accel_step_z : 0;

    for (int i = 0; i < max(x_steps, y_steps); i++) {
        if (X_CURR_SPEED > SPEED) X_CURR_SPEED -= dVx;
        else X_CURR_SPEED = SPEED;

        if (Y_CURR_SPEED > SPEED) Y_CURR_SPEED -= dVy;
        else Y_CURR_SPEED = SPEED;

        CURR_SPEED = (x_steps > y_steps) ? X_CURR_SPEED : Y_CURR_SPEED;

        if (i < x_steps) {
            digitalWrite(X1_STEP_PIN, HIGH);
            digitalWrite(X2_STEP_PIN, HIGH);
        }
        if (i < y_steps) digitalWrite(Y_STEP_PIN, HIGH);

        delayMicroseconds(CURR_SPEED);

        if (i < x_steps) {
            digitalWrite(X1_STEP_PIN, LOW);
            digitalWrite(X2_STEP_PIN, LOW);
        }
        if (i < y_steps) digitalWrite(Y_STEP_PIN, LOW);

        delayMicroseconds(CURR_SPEED);
    }
    
    for (int i = 0; i < z_steps; i++) {
      
      if (Z_CURR_SPEED > SPEED) Z_CURR_SPEED -= dVz;
      else Z_CURR_SPEED = SPEED;
      
      CURR_SPEED = Z_CURR_SPEED;
      
      digitalWrite(Z_STEP_PIN, HIGH);
      digitalWrite(Z_STEP_PIN, HIGH);
      delayMicroseconds(CURR_SPEED);
      digitalWrite(Z_STEP_PIN, LOW);
      digitalWrite(Z_STEP_PIN, LOW);
      delayMicroseconds(CURR_SPEED);
    }
    
}

//*******************************************************************************************************

void setup() {
  Serial.begin(115200);
  // Inicjowanie pinów
  pinMode(X1_STEP_PIN, OUTPUT);
  pinMode(X1_DIR_PIN, OUTPUT);
  pinMode(X2_STEP_PIN, OUTPUT);
  pinMode(X2_DIR_PIN, OUTPUT);
  pinMode(Y_STEP_PIN, OUTPUT);
  pinMode(Y_DIR_PIN, OUTPUT);
  pinMode(Z_STEP_PIN, OUTPUT);
  pinMode(Z_DIR_PIN, OUTPUT);
  pinMode(RELAY, OUTPUT);

  // Ustawianie prędkości silników
  
  digitalWrite(X1_STEP_PIN, LOW);
  digitalWrite(Y_STEP_PIN, LOW);
  digitalWrite(Z_STEP_PIN, LOW);
  delay(1000);

  // Ustawianie kierunku ruchu
  digitalWrite(X1_DIR_PIN, X1_FORWARD);
  digitalWrite(Y_DIR_PIN, Y_FORWARD);
  digitalWrite(Z_DIR_PIN, Z_FORWARD);
  delay(1000);

  // Inicjalizacja websocketu
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }
  
  // Ustawienie stałego IP
  WiFi.config(staticIP, gateway, subnet);
  
  Serial.println("Connected to WiFi");

  Serial.print("Connected to network with IP address: ");
  Serial.println(WiFi.localIP());

    /*use mdns for host name resolution*/
  if (!MDNS.begin(host)) { //http://esp32.local
    Serial.println("Error setting up MDNS responder!");
    while (1) {
      delay(1000);
    }
  }
  Serial.println("mDNS responder started");
  /*return index page which is stored in serverIndex */
  server.on("/", HTTP_GET, []() {
    server.sendHeader("Connection", "close");
    server.send(200, "text/html", loginIndex);
  });
  server.on("/serverIndex", HTTP_GET, []() {
    server.sendHeader("Connection", "close");
    server.send(200, "text/html", serverIndex);
  });
  /*handling uploading firmware file */
  server.on("/update", HTTP_POST, []() {
    server.sendHeader("Connection", "close");
    server.send(200, "text/plain", (Update.hasError()) ? "FAIL" : "OK");
    ESP.restart();
  }, []() {
    HTTPUpload& upload = server.upload();
    if (upload.status == UPLOAD_FILE_START) {
      Serial.printf("Update: %s\n", upload.filename.c_str());
      if (!Update.begin(UPDATE_SIZE_UNKNOWN)) { //start with max available size
        Update.printError(Serial);
      }
    } else if (upload.status == UPLOAD_FILE_WRITE) {
      /* flashing firmware to ESP*/
      if (Update.write(upload.buf, upload.currentSize) != upload.currentSize) {
        Update.printError(Serial);
      }
    } else if (upload.status == UPLOAD_FILE_END) {
      if (Update.end(true)) { //true to set the size to the current progress
        Serial.printf("Update Success: %u\nRebooting...\n", upload.totalSize);
      } else {
        Update.printError(Serial);
      }
    }
  });
  server.begin();
  
  webSocket.begin();
  webSocket.onEvent(webSocketEvent);
}

float x, y, z, f; // Zmienne do przechowywania wyników


//*******************************************************************************************************


void loop() {

  server.handleClient();
  webSocket.loop();
  digitalWrite(RELAY, LOW);

  
    if (strlen(receivedPayload) > 0) {
      
    // Przekształć tablicę receivedPayload w obiekt typu String
      String receivedString = String(receivedPayload);

    // Wywołanie funkcji parseGcode
      parseGcode(receivedString, x, y, z, f); 
      if (receivedString == "on"){
      move(x, y, z, f);
      
    }
    memset(receivedPayload, 0, sizeof(receivedPayload));
  }  
}

void webSocketEvent(uint8_t num, WStype_t type, uint8_t* payload, size_t length) {
    Serial.printf("WebSocket event: type %d, length %u\n", type, length);
    if (length > 0) {
      Serial.print("Payload: ");
      for (size_t i = 0; i < length; i++) {
        Serial.print((char)payload[i]);
        receivedPayload[i] = payload[i];
      }
      Serial.println();
   }
}
