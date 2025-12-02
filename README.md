[![YouTube Video](https://img.shields.io/badge/Watch%20on-YouTube-red?logo=youtube)](https://www.youtube.com/watch?v=1NbKHaOdFg4)


**Sena Sultan Karbuz 23370031039**

**Enes Furkan Kuş 23370031050**

** Dr.Öğretim Üyesi Hasan Serdar Hocamızın İşletim Sistemleri ve Bilgisayar Ağları Ödevi İçin **

Bilgisayar Ağları Ödevi : Mosquitto(MQTT)  Protokolu Kullanarak Veri Aktarımı Sağlamak 

İşletim Sistemleri Ödevi : Bir Task Manager Yapmak (burdaki verileride mqtt kullanarak aktardim)



# IoT Cihaz Metrik Paneli (MQTT ile)

Bu proje, bir NodeMCU (ESP8266) cihazının sistem metriklerini (RAM, CPU Yükü) bir ASP.NET Core paneli üzerinden MQTT aracılığıyla anlık olarak izlememizi sağlıyor.

Cihaz, bir potansiyometreden gelen değere göre otonom olarak kendi belleğini (`realloc` ile) ve işlemcisini (`loop` bloklaması ile) strese sokar.

## Teknik Yapı

* **Cihaz (Publisher):** NodeMCU (`PubSubClient`)
    * Metrikleri (heap, loop_time, cpu_load) ve potansiyometre değerini `cihaz/...` konularına(topic) yayınlar.
* **Aracı (Broker):** Mosquitto
    * Lokal ağda çalışır, kimlik doğrulama (`Ryuka/1661`) kullanır.
* **Backend (Subscriber):** ASP.NET Core MVC (`MQTTnet`)
    * `BackgroundService`, `cihaz/#` konusuna abone olur ve veriyi bir `Singleton` servise (`IMqttDataStore`) yazar.

## Proje Neden Sadece Lokal Ağda Çalışıyor?

Bu sistem, internet üzerinden erişime kapalıdır ve **sadece yerel ağda (Lokal IP)** çalışacak şekilde tasarlanmıştır.

1.  **Lokal Broker:** MQTT Broker'ımız (Mosquitto), `192.168.43.207` IP adresine sahip lokal bir bilgisayarda çalışmaktadır.
2.  **Sabit IP Yapılandırması:** Hem NodeMCU (`mqtt_server_ip`) hem de ASP.NET Core uygulaması (`appsettings.json`), *doğrudan bu lokal IP'ye* bağlanmak üzere kodlanmıştır.

Bu mimariden dolayı, NodeMCU'nun ve ASP.NET Core sunucusunun çalışması için **her ikisinin de** Broker'ın bulunduğu bilgisayar ile **aynı WiFi ağına bağlı olması zorunludur.** 

Bu sebeple kendi bilgisayarınızda çalısmak için parametreleri değiştirmeniz gerekli
