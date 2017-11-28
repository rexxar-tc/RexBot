using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using DSharpPlus.Entities;

namespace RexBot.Commands
{
    class CommandGraph : IChatCommand
    {
        public CommandAccess Access => CommandAccess.Rexxar;
        public string Command => "!graph";
        public string HelpText => "Takes a mathematical function and graphs it.";
        public DiscordEmbed HelpEmbed { get; }

        public async Task<string> Handle(DiscordMessage message)
        {
            var d = new List<DataPoint>();
            for (int i = 0; i < 10; i++)
                d.Add(new DataPoint(i, i));
            using (var s = new MemoryStream())
            {
                GeneratePlot(d, s);
                s.Position = 0;
                await message.Channel.SendFileAsync(s, "graph.jpg");
            }

            return "ok";
        }

        public void GeneratePlot(IList<DataPoint> series, Stream outputStream)
        {
            using (var ch = new Chart())
            {
                ch.ChartAreas.Add(new ChartArea());
                
                var s = new Series();
                s.ChartType = SeriesChartType.FastLine;
                foreach (var pnt in series) s.Points.Add(pnt);
                ch.Series.Add(s);
                ch.SaveImage(outputStream, ChartImageFormat.Jpeg);
            }
        }
    }
}
