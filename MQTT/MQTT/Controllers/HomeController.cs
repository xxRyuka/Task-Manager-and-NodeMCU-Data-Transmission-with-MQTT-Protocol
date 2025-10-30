using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MQTT.Models;
using MQTT.Services.MqttDataStore;

namespace MQTT.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMqttDataStore _dataStore;

    public HomeController(ILogger<HomeController> logger, IMqttDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;
    }

    public IActionResult Index()
    {
        ViewBag.LatestPotValue = _dataStore.LatestPotValue ?? "Veri yok";
        return View();
    }

    // --- YENİ API ACTION ---
    // Burası JavaScript tarafından her saniye çağrılacak
    [HttpGet]
    public IActionResult GetLatestPotValue()
    {
        var value = _dataStore.LatestPotValue ?? "N/A";
        // Veriyi JSON formatında döndürüyoruz
        return Json(new { success = true, value = value });
    }

    
    
    // --- YENİ EKLENEN METOT ---
    // BU METOT SADECE VERİ DÖNER (JSON)
    [HttpGet] // Bu metodun bir GET isteği olduğunu belirtir
    public IActionResult GetLatestStats()
    {
        // Singleton servisimizden o anki en son verileri çek
        var data = new
        {
            heap = _dataStore.LastHeapValue,
            cpuFreq = _dataStore.LastCpuFreqValue,
            loopTime = _dataStore.LastLoopTime
        };

        // Veriyi JSON formatında döndür
        return Json(data);
    }
// YENİ EKLENEN METOT
    public IActionResult TaskManager()
    {
        // Verileri depomuzdan çekip View'a gönder
        ViewData["Heap"] = _dataStore.LastHeapValue;
        ViewData["CpuFreq"] = _dataStore.LastCpuFreqValue;
        ViewData["LoopTime"] = _dataStore.LastLoopTime;
    
        return View(); // TaskManager.cshtml dosyasını arayacak
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}