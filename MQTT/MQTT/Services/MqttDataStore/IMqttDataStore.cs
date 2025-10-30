namespace MQTT.Services.MqttDataStore;

// Bu arayüz, veriye nasıl erişeceğimizi tanımlar
public interface IMqttDataStore
{
    string? LatestPotValue { get; }
    void UpdatePotValue(string newValue);


    // YENİ EKLENENLER
    string LastHeapValue { get; }
    void UpdateHeapValue(string newValue);

    string LastCpuFreqValue { get; }
    void UpdateCpuFreqValue(string newValue);


    string LastLoopTime { get; }
    void UpdateLoopTime(string newValue);
}