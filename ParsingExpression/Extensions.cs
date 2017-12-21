using ParsingExpression.Automaton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ParsingExpression
{
    static class Extensions
    {
        public static void SaveGraphToFile(this IFsm fsm, string fileName)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                stream.SetLength(0);
                new XmlSerializer(typeof(Dgml.DirectedGraph)).Serialize(stream, fsm.BuildGraph().ToDgml());
            }
        }
            
    }
}
