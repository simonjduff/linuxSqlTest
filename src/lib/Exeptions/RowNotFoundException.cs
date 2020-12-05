using System;

namespace lib.Exeptions
{
    public class RowNotFoundException : Exception
    {
        public RowNotFoundException() : base("No row returned")
        {
            
        }
    }
}