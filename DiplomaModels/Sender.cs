using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiplomaModels.Interface;

namespace DiplomaModels
{
    public class Sender : IUser
    {
        private string _name;

        private string _nameNormalized;

        public int Id { get; set; }
        public string Name {
            get { return _name; }
            set {
                _name = value;
                _nameNormalized = value.ToUpper().Replace(" ", "");
            }
        }
        public string NameNormalized {
            get { return _nameNormalized; }
           set {  _nameNormalized = value;}
            }
    }
}
