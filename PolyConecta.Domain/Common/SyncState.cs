namespace PolyConecta.Domain.Common;

public enum SyncStatus
{
    NoAplica,
    Pendiente,
    Enviado,
    Confirmado,
    Error,
}

/// <summary>
/// Estado de sincronización con CONTPAQi de un documento (CT-13, CT-15). Es independiente de su
/// estado de negocio: un traslado puede estar Hecho y su sincronización en Error.
/// </summary>
public sealed class SyncState
{
    public SyncStatus Status { get; private set; } = SyncStatus.NoAplica;

    /// <summary>Folio que devuelve el bridge.</summary>
    public string? ErpFolio { get; private set; }

    /// <summary>Id del documento en CONTPAQi.</summary>
    public string? ErpId { get; private set; }

    /// <summary>`documentos[]` del resultado, en JSON (la salida y la entrada de un TRASPASO).</summary>
    public string? ErpDocuments { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public DateTimeOffset? LastSyncAt { get; private set; }

    /// <summary>El documento encoló un comando para CONTPAQi.</summary>
    public void MarcarPendiente(DateTimeOffset cuando)
    {
        Exigir(SyncStatus.Pendiente, SyncStatus.NoAplica, SyncStatus.Confirmado);
        Status = SyncStatus.Pendiente;
        LastSyncAt = cuando;
    }

    /// <summary>El bridge aceptó el comando (202).</summary>
    public void MarcarEnviado(DateTimeOffset cuando)
    {
        Exigir(SyncStatus.Enviado, SyncStatus.Pendiente);
        Status = SyncStatus.Enviado;
        LastSyncAt = cuando;
    }

    /// <summary>
    /// Callback CONFIRMED. Devuelve false si el documento ya estaba confirmado: un callback
    /// repetido o tardío no cambia nada (contrato §3, "Orden").
    /// </summary>
    public bool Confirmar(string? folio, string? idErp, string? documentos, DateTimeOffset cuando)
    {
        if (Status == SyncStatus.Confirmado) return false;
        Exigir(SyncStatus.Confirmado, SyncStatus.Enviado, SyncStatus.Pendiente);
        Status = SyncStatus.Confirmado;
        ErpFolio = folio;
        ErpId = idErp;
        ErpDocuments = documentos;
        LastErrorCode = null;
        LastErrorMessage = null;
        LastSyncAt = cuando;
        return true;
    }

    /// <summary>Callback FAILED o DEAD_LETTER, o reintentos de red agotados.</summary>
    public bool MarcarError(string codigo, string mensaje, DateTimeOffset cuando)
    {
        if (Status == SyncStatus.Confirmado) return false;
        Exigir(SyncStatus.Error, SyncStatus.Enviado, SyncStatus.Pendiente, SyncStatus.Error);
        Status = SyncStatus.Error;
        LastErrorCode = codigo;
        LastErrorMessage = mensaje;
        LastSyncAt = cuando;
        return true;
    }

    /// <summary>Sistemas reintenta un documento en Error (CT-20): vuelve a Pendiente.</summary>
    public void Reintentar(DateTimeOffset cuando)
    {
        Exigir(SyncStatus.Pendiente, SyncStatus.Error);
        Status = SyncStatus.Pendiente;
        LastSyncAt = cuando;
    }

    private void Exigir(SyncStatus destino, params SyncStatus[] desde)
    {
        if (!desde.Contains(Status))
            throw new TransicionInvalidaException(nameof(SyncState), Status.ToString(), destino.ToString());
    }
}

/// <summary>Documento que escribe en CONTPAQi (CT-13).</summary>
public interface ISyncedDocument
{
    long Id { get; }

    SyncState Sync { get; }
}
