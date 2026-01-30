public class SPResponse<T>
{
    public int Codigo { get; set; } // 0 = OK, -1 = Error
    public string Mensaje { get; set; }
    public T? Data { get; set; }
}
