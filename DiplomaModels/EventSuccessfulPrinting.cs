using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomaModels
{
    public class EventSuccessfulPrinting
    {
        public int Id { get; set; }
        public DateTime? DateTime { get; set; }
        public int? SenderId { get; set; }
        public virtual Sender? Sender { get; set; }
        public int? SenderDeviceId { get; set; }
        public virtual SenderDevice? SenderDevice { get; set; }
        public int? SentPrintingFileId { get; set; }
        public virtual SentPrintingFile? SentPrintingFile { get; set; }

    }
}
