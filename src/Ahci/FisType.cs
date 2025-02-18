namespace NoteOS.Ahci;

public enum FisType
{
    /// <summary>
    /// Register FIS - host to device
    /// </summary>
    FIS_TYPE_REG_H2D = 0x27,
    /// <summary>
    /// Register FIS - device to host
    /// </summary>
    FIS_TYPE_REG_D2H = 0x34,
    /// <summary>
    /// DMA activate FIS - device to host
    /// </summary>
    FIS_TYPE_DMA_ACT = 0x39,
    /// <summary>
    /// DMA setup FIS - bidirectional
    /// </summary>
    FIS_TYPE_DMA_SETUP = 0x41,
    /// <summary>
    /// Data FIS - bidirectional
    /// </summary>
    FIS_TYPE_DATA = 0x46,
    /// <summary>
    /// BIST activate FIS - bidirectional
    /// </summary>
    FIS_TYPE_BIST = 0x58,
    /// <summary>
    /// PIO setup FIS - device to host
    /// </summary>
    FIS_TYPE_PIO_SETUP = 0x5F,
    /// <summary>
    /// Set device bits FIS - device to host
    /// </summary>
    FIS_TYPE_DEV_BITS = 0xA1,
}