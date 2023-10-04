using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TableroApuestas.Models
{
    public class Usuario
    {
        public int ID { get; set; }
        public string? nombre { get; set; }
        public string? apellido { get; set; }
        public string? password { get; set; }
    }
}
