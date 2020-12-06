using System.Threading;

namespace lib
{
    public class State
    {
        public int Id { get; set; }
        public AutoResetEvent AutoResetEvent { get; set; }
    }
}