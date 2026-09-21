namespace Books.Models;

/// <summary>
/// Контракт кодов результата хранимых процедур. 
/// </summary>
public enum OperationCode : byte
{
    NotFound = 0,
    Success = 1,
    Duplicate = 2
}