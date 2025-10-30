using System.Text;
using Microsoft.Extensions.Options;
using MQTT.Services.MqttDataStore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MQTT.Services;

// Servisin adını daha açıklayıcı hale getirebiliriz
public class MqttBackgroundService : BackgroundService
{
    private readonly ILogger<MqttBackgroundService> _logger;
    private readonly IMqttDataStore _dataStore;
    private readonly IManagedMqttClient _mqttClient;
    private readonly MqttSettings _mqttSettings;

    public MqttBackgroundService(
        ILogger<MqttBackgroundService> logger,
        IMqttDataStore dataStore,
        IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _dataStore = dataStore;
        _mqttSettings = mqttSettings.Value; // appsettings'den gelen ayarlar

        // MQTT İstemcisini oluştur
        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        // İstemci olaylarını ayarla
        _mqttClient.ConnectedAsync += OnConnected;
        _mqttClient.DisconnectedAsync += OnDisconnected;
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Arka Plan Servisi başlıyor...");

        // Ayarları appsettings.json'dan okuyarak seçenekleri oluştur
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithClientId(_mqttSettings.ClientId)
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .WithCredentials(_mqttSettings.Username, _mqttSettings.Password) // KULLANICI ADI/ŞİFRE
                .WithCleanSession()
                .Build())
            .Build();

        await _mqttClient.StartAsync(options);

        _logger.LogInformation("MQTT İstemcisi başlatıldı. Bağlantı bekleniyor...");

        // Servisin kapanmasını engelle
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task OnConnected(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation("MQTT Broker'a başarıyla bağlandı.");

        // "cihaz/" ile başlayan her şeye abone ol
        string topic = "cihaz/#";

        var topicFilter = new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .Build();

        // Metot bir DİZİ (IEnumerable) bekler
        await _mqttClient.SubscribeAsync(new[] { topicFilter });

        _logger.LogInformation("'{Topic}' konusuna abone olundu.", topic);
    }

    private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogWarning(e.Exception, "MQTT Broker ile bağlantı koptu! Yeniden bağlanılmaya çalışılacak...");
        return Task.CompletedTask;
    }

    // Gelen tüm mesajlar buraya düşer
    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _logger.LogInformation("Yeni mesaj alındı. Konu: {Topic}, Mesaj: {Payload}", topic, payload);

        // Gelen veriyi konuya (topic) göre ilgili servise yönlendir
        try
        {
            switch (topic)
            {
                case "cihaz/pot/deger":
                    _dataStore.UpdatePotValue(payload);
                    break;
                case "cihaz/nodemcu/stats/heap":
                    _dataStore.UpdateHeapValue(payload);
                    break;
                case "cihaz/nodemcu/stats/cpu_freq":
                    _dataStore.UpdateCpuFreqValue(payload);
                    break;
                case "cihaz/nodemcu/stats/loop_time_us":
                    _dataStore.UpdateLoopTime(payload);
                    break;
                default:
                    _logger.LogWarning("Bilinmeyen bir konu (topic) için veri alındı: {Topic}", topic);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj işlenirken hata oluştu. Konu: {Topic}", topic);
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MQTT Servisi durduruluyor...");
        await _mqttClient.StopAsync(true);
        await base.StopAsync(cancellationToken);
    }
}

public class MqttSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientId { get; set; } = "ASPNET_Core_Client";
}