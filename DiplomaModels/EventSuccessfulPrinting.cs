using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomaModels
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class EventSuccessfulPrinting
    {
        /// <summary>
        /// Идентификатор события.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Дата и время события.
        /// </summary>
        public DateTime? DateTime { get; set; }

        /// <summary>
        /// Идентификатор отправителя.
        /// </summary>
        public int? SenderId { get; set; }

        /// <summary>
        /// Объект отправителя.
        /// </summary>
        [ForeignKey("SenderId")]
        public virtual Sender? Sender { get; set; }

        /// <summary>
        /// Идентификатор устройства отправителя.
        /// </summary>
        public int? SenderDeviceId { get; set; }

        /// <summary>
        /// Объект устройства отправителя.
        /// </summary>
        [ForeignKey("SenderDeviceId")]
        public virtual SenderDevice? SenderDevice { get; set; }

        /// <summary>
        /// Идентификатор целевого принтера.
        /// </summary>
        public int? TargetPrinterId { get; set; }

        /// <summary>
        /// Объект целевого принтера.
        /// </summary>
        [ForeignKey("TargetPrinterId")]
        public virtual TargetPrinter? TargetPrinter { get; set; }

        /// <summary>
        /// Идентификатор отправленного файла для печати.
        /// </summary>
        public int? SentPrintingFileId { get; set; }

        /// <summary>
        /// Объект отправленного файла для печати.
        /// </summary>
        [ForeignKey("SentPrintingFileId")]
        public virtual SentPrintingFile? SentPrintingFile { get; set; }

        /// <summary>
        /// Конструктор для инициализации всех свойств класса.
        /// </summary>
        /// <param name="dateTime">Дата и время события.</param>
        /// <param name="sender">Отправитель</param>
        /// <param name="senderDevice">Устройство с которого был отправлен файл</param>
        /// <param name="targetPrinter">Принтер на который был отправлен файл</param>
        /// <param name="sentPrintingFile">Отправленный файл</param>
        public EventSuccessfulPrinting(DateTime? dateTime, Sender? sender,
                                       SenderDevice? senderDevice, TargetPrinter? targetPrinter,
                                       SentPrintingFile? sentPrintingFile)
        {
            DateTime = dateTime;
            Sender = sender;
            SenderDevice = senderDevice;
            TargetPrinter = targetPrinter;
            SentPrintingFile = sentPrintingFile;
        }

        // Parameterless constructor required by EF Core
        public EventSuccessfulPrinting()
        {
        }
    }


}
