using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomaModels
{
    public class SentPrintingFile
    {
        public SentPrintingFile() { }
        public SentPrintingFile( string name, long size, short pages)
        {
            
            Name = new DocumentName
            {
                name = name
            };
            Size = size;
            Pages = pages;
        }

        public int Id { get; set; }
        public DocumentName Name { get; set; }
        public long Size { get; set; } 
        public short Pages { get; set; }
    }
}
