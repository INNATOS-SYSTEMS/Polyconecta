using System.Text.Json;
using Contpaq.Bridge.Infrastructure.Sdk;
using SdkLab;

// Contrato para el agente: stdout = UN objeto JSON {ok, command, result|error}. Código de salida 0 si ok.
// Toda invocación queda además en evidence/log-YYYYMMDD.jsonl.

const string Usage = """
sdklab <comando> [args]
  env                                  Diagnóstico: x86, DLL, versión del SDK, guardas del laboratorio (no abre la empresa)
  guard                                Solo evalúa las guardas
  sdk-error <rc>                       Texto de fError para un código (A-02)
  sdk-open [ruta-empresa]              fInicializaSDK + fAbreEmpresa + cierre (A-01). Con ruta distinta a la de laboratorio SOLO se permite una inexistente (provocar error, A-02)
  sql "<SELECT ...>"                   Consulta de solo lectura sobre la base de laboratorio
  snapshot <nombre> <prod> [<prod>..]  Guarda existencias + capas + almacenes de esos SKU en evidence/snapshots/<nombre>.json
  diff <a> <b>                         Diferencias entre dos snapshots (capas y existencias)
  run <spec.json>                      Ejecuta un experimento de documento (ver specs/*.json)
  afecta <concepto> <serie> <folio> <true|false>   fAfectaDocto_Param suelto (B-04, D-04)
  pendientes <concepto> <producto> <almacen>       fObtieneUnidadesPendientes (E-02)
""";

if (args.Length == 0 || args[0] is "-h" or "--help") { Console.WriteLine(Usage); return 0; }

var command = args[0];
var rest = args.Skip(1).ToArray();
LabConfig? cfg = null;

try
{
    cfg = LabConfig.Load();
    object result = command switch
    {
        "env" => Env(cfg),
        "guard" => Guard.Evaluate(cfg),
        "sdk-error" => new { rc = int.Parse(Need(rest, 0, "rc")), message = ContpaqiSdkNative.GetErrorMessage(int.Parse(rest[0])) },
        "sdk-open" => SdkOpen(cfg, rest.FirstOrDefault()),
        "sql" => Guarded(cfg, () => SqlTools.Query(cfg, Need(rest, 0, "sql"))),
        "snapshot" => Guarded(cfg, () => SaveSnapshot(cfg, Need(rest, 0, "nombre"), rest.Skip(1).ToArray())),
        "diff" => Diff(cfg, Need(rest, 0, "a"), Need(rest, 1, "b")),
        "run" => Guarded(cfg, () => SpecRunner.Run(cfg, LoadSpec(Need(rest, 0, "spec.json")))),
        "afecta" => Guarded(cfg, () => SpecRunner.Afecta(cfg, Need(rest, 0, "concepto"), Need(rest, 1, "serie"), double.Parse(Need(rest, 2, "folio")), bool.Parse(Need(rest, 3, "true|false")))),
        "pendientes" => Guarded(cfg, () => SpecRunner.Pendientes(cfg, Need(rest, 0, "concepto"), Need(rest, 1, "producto"), Need(rest, 2, "almacen"))),
        _ => throw new LabException("UNKNOWN_COMMAND", Usage),
    };
    Emit(cfg, command, rest, new { ok = true, command, result });
    return 0;
}
catch (Exception e)
{
    var code = e is LabException le ? le.Code : e.GetType().Name;
    Emit(cfg, command, rest, new { ok = false, command, error = new { code, message = e.Message } });
    return e is LabException { Code: "GUARD_REFUSED" } ? 3 : 1;
}

static void Emit(LabConfig? cfg, string command, string[] rest, object payload)
{
    Console.WriteLine(JsonSerializer.Serialize(payload, Json.Options));
    Evidence.Append(cfg, command, rest, payload);
}

static string Need(string[] a, int i, string name) =>
    a.Length > i ? a[i] : throw new LabException("ARGS", $"Falta el argumento <{name}>.\n{Usage}");

static T Guarded<T>(LabConfig cfg, Func<T> body) { Guard.Require(cfg); return body(); }

static DocSpec LoadSpec(string path) =>
    JsonSerializer.Deserialize<DocSpec>(File.ReadAllText(path), Json.Options) ?? throw new LabException("SPEC_INVALID", "spec vacío");

static object Env(LabConfig cfg)
{
    var dll = Path.Combine(cfg.SdkPath, ContpaqiSdkNative.DllName);
    string? version = null;
    if (File.Exists(dll)) version = System.Diagnostics.FileVersionInfo.GetVersionInfo(dll).FileVersion;
    var exe = Directory.Exists(cfg.SdkPath)
        ? Directory.GetFiles(cfg.SdkPath, "*.exe").Select(f => new { file = Path.GetFileName(f), version = System.Diagnostics.FileVersionInfo.GetVersionInfo(f).FileVersion }).Take(10).ToArray()
        : null;
    return new
    {
        process32bit = IntPtr.Size == 4,
        os = Environment.OSVersion.ToString(),
        sdkPath = cfg.SdkPath, sdkDllPresent = File.Exists(dll), sdkDllVersion = version, sdkFolderExecutables = exe,
        labCompanyPath = cfg.LabCompanyPath, labCompanyDirExists = Directory.Exists(cfg.LabCompanyPath),
        guard = Guard.Evaluate(cfg),
    };
}

static object SdkOpen(LabConfig cfg, string? overridePath)
{
    if (overridePath is not null)
    {
        // A-02: provocar un error conocido. Solo con una ruta que NO exista, jamás con otra empresa real.
        if (Directory.Exists(overridePath))
            throw new LabException("GUARD_REFUSED", "sdk-open <ruta> solo acepta rutas inexistentes (para provocar error). La empresa de laboratorio se abre sin argumento.");
        return SdkSessionProbe(cfg, overridePath);
    }
    Guard.Require(cfg);
    return SdkSessionProbe(cfg, cfg.LabCompanyPath);
}

static object SdkSessionProbe(LabConfig cfg, string companyPath)
{
    var probe = new LabConfig { SdkPath = cfg.SdkPath, LabCompanyPath = companyPath, LabDatabase = cfg.LabDatabase, SqlServer = cfg.SqlServer };
    try { return SdkSession.Run(probe, () => new { opened = true, companyPath }); }
    catch (LabException e) when (e.Code == "SDK_OPEN_FAILED") { return new { opened = false, companyPath, error = e.Message }; }
}

static object SaveSnapshot(LabConfig cfg, string name, string[] productos)
{
    var snap = SqlTools.Snapshot(cfg, productos);
    var dir = Path.Combine(cfg.ResolveEvidenceDir(), "snapshots");
    Directory.CreateDirectory(dir);
    var path = Path.Combine(dir, name + ".json");
    File.WriteAllText(path, JsonSerializer.Serialize(new { name, ts = DateTimeOffset.Now, productos, data = snap }, Json.Options));
    return new { path, capas = ((System.Collections.ICollection)snap["capas"]).Count, existencias = ((System.Collections.ICollection)snap["existencias"]).Count };
}

static object Diff(LabConfig cfg, string a, string b)
{
    List<JsonElement> Load(string n, string section)
    {
        var p = Path.Combine(cfg.ResolveEvidenceDir(), "snapshots", n + ".json");
        if (!File.Exists(p)) throw new LabException("SNAPSHOT_MISSING", p);
        return JsonDocument.Parse(File.ReadAllText(p)).RootElement.GetProperty("data").GetProperty(section).EnumerateArray().Select(e => e.Clone()).ToList();
    }
    string Get(JsonElement e, string k) => e.TryGetProperty(k, out var v) ? v.ToString() : "";

    // Capas: clave (producto, almacén, idCapa) → existencia
    var capasA = Load(a, "capas").ToDictionary(e => $"{Get(e, "producto")}|{Get(e, "almacen")}|capa {Get(e, "CIDCAPA")}|lote {Get(e, "CNUMEROLOTE")}");
    var capasB = Load(b, "capas").ToDictionary(e => $"{Get(e, "producto")}|{Get(e, "almacen")}|capa {Get(e, "CIDCAPA")}|lote {Get(e, "CNUMEROLOTE")}");
    var changes = new List<object>();
    foreach (var k in capasA.Keys.Union(capasB.Keys).OrderBy(x => x))
    {
        var before = capasA.TryGetValue(k, out var ea) ? Get(ea, "CEXISTENCIA") : null;
        var after = capasB.TryGetValue(k, out var eb) ? Get(eb, "CEXISTENCIA") : null;
        if (before != after)
            changes.Add(new { capa = k, antes = before ?? "(no existía)", despues = after ?? "(desapareció)", origen = eb.ValueKind == JsonValueKind.Undefined ? null : Get(eb, "CIDCAPAORIGEN") });
    }

    // Existencia neta por (producto, almacén): entradas - salidas de todos los periodos del ejercicio no está en el snapshot resumido;
    // se compara la fila completa serializada, y el agente interpreta las columnas CENTRADASPERIODO*/CSALIDASPERIODO*.
    var exA = Load(a, "existencias").ToDictionary(e => $"{Get(e, "producto")}|{Get(e, "almacen")}|{Get(e, "ejercicio")}", e => e.GetRawText());
    var exB = Load(b, "existencias").ToDictionary(e => $"{Get(e, "producto")}|{Get(e, "almacen")}|{Get(e, "ejercicio")}", e => e.GetRawText());
    var exChanged = exA.Keys.Union(exB.Keys).Where(k => exA.GetValueOrDefault(k) != exB.GetValueOrDefault(k)).OrderBy(x => x)
        .Select(k => new { fila = k, antes = exA.GetValueOrDefault(k), despues = exB.GetValueOrDefault(k) }).ToArray();

    return new { capasCambiadas = changes, existenciasCambiadas = exChanged };
}
