namespace MQTT.Services.MqttDataStore;

// Bu sınıf, veriyi hafızada (RAM) tutar

// Thread-safe (aynı anda birden fazla isteği) yönetmek için 'lock' kullanıyoruz
public class InMemoryMqttDataStore : IMqttDataStore
{
// Tüm metrikler için varsayılan değerler
    public string LatestPotValue { get; private set; } = "Bekleniyor...";
    public string LastHeapValue { get; private set; } = "Bekleniyor...";
    public string LastCpuFreqValue { get; private set; } = "Bekleniyor...";
    public string LastLoopTime { get; private set; } = "Bekleniyor...";

    // Potansiyometre
    public void UpdatePotValue(string newValue)
    {
        LatestPotValue = newValue;
    }

    // Boş Hafıza (Heap)
    public void UpdateHeapValue(string newValue)
    {
        if (long.TryParse(newValue, out long heapBytes))
        {
            // Gelen byte değerini KB'a çevir
            LastHeapValue = $"{(heapBytes / 1024.0):F2} KB";
        }
        else
        {
            LastHeapValue = newValue;
        }
    }

    // CPU Frekansı
    public void UpdateCpuFreqValue(string newValue)
    {
        LastCpuFreqValue = $"{newValue} MHz";
    }

    // Döngü Süresi
    public void UpdateLoopTime(string newValue)
    {
        LastLoopTime = $"{newValue} µs";
    }
}