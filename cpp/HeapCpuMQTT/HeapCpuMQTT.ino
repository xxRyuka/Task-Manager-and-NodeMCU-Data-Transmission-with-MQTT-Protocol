#include <ESP8266WiFi.h>
#include <PubSubClient.h>

// --- AYARLAR BAŞLANGIÇ ---

// 1. WiFi Ayarları (Sizin)
const char* ssid = "smd";
const char* password = "furkan16";

// 2. MQTT Broker Ayarları (Sizin)
const char* mqtt_server_ip = "192.168.43.207";
const int mqtt_port = 1883;
const char* mqtt_user = "Ryuka";
const char* mqtt_pass = "1661";

// 3. MQTT Konu (Topic) Ayarı (Sizin)
const char* pot_topic = "cihaz/pot/deger";
const char* heap_topic = "cihaz/nodemcu/stats/heap";
const char* cpu_topic = "cihaz/nodemcu/stats/cpu_freq";
const char* loop_time_topic = "cihaz/nodemcu/stats/loop_time_us";

// --- AYARLAR BİTİŞ ---

const int potPin = A0;

WiFiClient espClient;
PubSubClient client(espClient);

long lastMsg = 0;
long lastStatsMsg = 0;
char msg[50];

// --- STRES TESTİ İLE İLGİLİ DEĞİŞİKLİKLER BAŞLANGIÇ ---
uint32_t loopTimeMicros = 0;
byte* memoryStressBuffer = NULL;
bool cpuTestWasRun = false;
// O anki bellek boyutunu takip etmek için eklendi
int currentAllocationSize = 0;
// --- STRES TESTİ İLE İLGİLİ DEĞİŞİKLİKLER BİTİŞ ---


void setup() {
  // ... (setup fonksiyonunuz - DOKUNULMADI) ...
  Serial.begin(115200);
  pinMode(potPin, INPUT);
  setup_wifi();
  client.setServer(mqtt_server_ip, mqtt_port);
  client.setCallback(callback);
}

void setup_wifi() {
  // ... (setup_wifi fonksiyonunuz - DOKUNULMADI) ...
  delay(10);
  Serial.println();
  Serial.print("Baglaniliyor: ");
  Serial.println(ssid);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");
  Serial.println("WiFi baglandi!");
  Serial.println("IP adresi: ");
  Serial.println(WiFi.localIP());
}

void callback(char* topic, byte* payload, unsigned int length) {
  // ... (callback fonksiyonunuz - DOKUNULMADI) ...
}

void reconnect() {
  // ... (reconnect fonksiyonunuz - DOKUNULMADI) ...
  while (!client.connected()) {
    Serial.print("MQTT baglantisi deneniyor...");
    String clientId = "NodeMCU-Potansiyometre";  // Sizin ID'niz
    if (client.connect(clientId.c_str(), mqtt_user, mqtt_pass)) {
      Serial.println("baglandi!");
    } else {
      Serial.print("basarisiz, rc=");
      Serial.print(client.state());
      Serial.println(" 5 saniye icinde tekrar denenecek");
      delay(5000);
    }
  }
}


void updateMemoryAllocation(int desiredSize) {
  // 1. İstenen boyut zaten ayırdığımızla aynıysa, hiçbir şey yapma.
  if (desiredSize == currentAllocationSize) {
    return;
  }

  // 2. Boyut 0'a düştüyse (stres bittiyse), belleği serbest bırak.
  if (desiredSize == 0) {
    if (memoryStressBuffer != NULL) {
      free(memoryStressBuffer);
      memoryStressBuffer = NULL;
      Serial.println("Bellek serbest bırakıldı.");
    }
  }
  // 3. Bellek ayır VEYA yeniden boyutlandır.
  else {
    byte* newBuffer = (byte*)realloc(memoryStressBuffer, desiredSize);

    if (newBuffer != NULL) {
      // Başarılı: Yeni tamponu ve boyutu kaydet
      memoryStressBuffer = newBuffer;
      Serial.print(desiredSize);
      Serial.println(" byte bellek ayrıldı (realloc).");
    } else {
      // Başarısız! (İstenen RAM çok fazlaydı)
      Serial.print(desiredSize);
      Serial.println(" byte AYRILAMADI! (realloc fail)");
      return;  // Fonksiyondan çık, o anki boyutu koru
    }
  }

  // İşlem başarılıysa, o anki boyutu güncelle
  currentAllocationSize = desiredSize;
}

// CPU stres fonksiyonu (Sizden alındı - DOKUNULMADI)
void performCpuStress() {
  Serial.println("CPU stres testi başladı...");
  long startTime = millis();
  volatile float f = 1.234;
  while (millis() - startTime < 50) {
    f = f * 1.01 + 0.01;
  }
  Serial.println("CPU stres testi bitti.");
}
// --- STRES TESTİ YARDIMCI FONKSİYONLARI BİTİŞ ---


void loop() {
  uint32_t loopStartMicros = micros();

  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  long now = millis();

  // KRİTİK HATA UYARISI: Bu blok saniyede 1000 kez çalışacak.
  if (now - lastMsg > 1) {  // SİZİN KODUNUZ - DOKUNULMADI
    lastMsg = now;

    int potValue = analogRead(potPin);

    // --- KARAR MEKANİZMASI (İSTEĞİNİZ ÜZERİNE GÜNCELLENDİ) ---
    // (Sizin kodunuzdaki dağınık allocateMemory çağrıları yerine)

    int desiredRam = 0;  // Varsayılan: 0 byte (stres yok)

    if (potValue >= 450 && potValue < 800) {
      // RAM'i 5KB (5000) ile 20KB (20000) arasında "hassas" olarak ayarla
      desiredRam = map(potValue, 450, 500, 5000, 20000);
    } else if (potValue >= 200 && potValue < 450) {
      // RAM'i 20KB (20001) ile 35KB (35000) arasında ayarla
      desiredRam = map(potValue, 200, 450, 3000, 5000);
    } else if (potValue >= 800 && potValue <= 1024) {
      // RAM'i 20KB (20001) ile 35KB (35000) arasında ayarla
      desiredRam = map(potValue, 800, 1024, 20001, 35000);
    }

    else if (potValue >= 0 && potValue < 200) {
      // RAM'i 20KB (20001) ile 35KB (35000) arasında ayarla
      desiredRam = map(potValue, 0, 200, 2, 3000);
    }
    // Diğer tüm aralıklarda desiredRam 0 olarak kalacak.

    // 1. Yeni akıllı RAM fonksiyonumuzu çağır
    updateMemoryAllocation(desiredRam);

    // 2. CPU Stresini ayrıca tetikle (Sadece 800-1024 aralığında)
    if (potValue >= 800 && potValue <= 1024) {
      if (!cpuTestWasRun) {
        performCpuStress();
        cpuTestWasRun = true;
      }
    } else {
      cpuTestWasRun = false;
    }
    // --- KARAR MEKANİZMASI BİTTİ ---


    // Değeri bir karakter dizisine (string) dönüştür
    snprintf(msg, 50, "%d", potValue);

    // Değeri MQTT Broker'a, belirlediğimiz konuya (topic) yayınla
    Serial.print("Mesaj yayinlaniyor: ");
    Serial.print(pot_topic);
    Serial.print(" -> ");
    Serial.println(msg);
    client.publish(pot_topic, msg);


    // KRİTİK HATA UYARISI: Bu blok da saniyede 1000 kez çalışacak.
    if (now - lastStatsMsg > 1) {  // SİZİN KODUNUZ - DOKUNULMADI
      lastStatsMsg = now;

      // Boş Hafıza (Heap) verisini gönder
      uint32_t freeHeap = ESP.getFreeHeap();
      snprintf(msg, 50, "%u", freeHeap);  // %u (unsigned int)
      Serial.print(heap_topic);
      Serial.print(" -> ");
      Serial.println(msg);
      client.publish(heap_topic, msg);

      // CPU Frekans verisini gönder
      uint8_t cpuFreq = ESP.getCpuFreqMHz();
      snprintf(msg, 50, "%u", cpuFreq);  // %u (unsigned int)
      Serial.print(cpu_topic);
      Serial.print(" -> ");
      Serial.println(msg);
      client.publish(cpu_topic, msg);

      // Döngü Yürütme Süresi(Loop Time) verisini gönder
      snprintf(msg, 50, "%u", loopTimeMicros);
      Serial.print(loop_time_topic);
      Serial.print(" -> ");
      Serial.println(msg);
      client.publish(loop_time_topic, msg);
    }
  }
  loopTimeMicros = micros() - loopStartMicros;
}