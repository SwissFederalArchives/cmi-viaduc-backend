using System.Linq;

namespace CMI.Contract.Asset
{
    public class ChannelAssignmentDefinition
    {
        public string Channel1 { get; set; }
        public string Channel2 { get; set; }
        public string Channel3 { get; set; }
        public string Channel4 { get; set; }

        /// <summary>
        ///     Returns the priority categories that are allowed for a given channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public int[] GetPrioritiesForChannel(int channel)
        {
            switch (channel % 4)
            {
                case 0:
                    return Channel4.Split(',').Select(n => int.Parse(n.Trim())).ToArray();
                case 1:
                    return Channel1.Split(',').Select(n => int.Parse(n.Trim())).ToArray();
                case 2:
                    return Channel2.Split(',').Select(n => int.Parse(n.Trim())).ToArray();
                case 3:
                    return Channel3.Split(',').Select(n => int.Parse(n.Trim())).ToArray();
                default:
                    return new int[] { };
            }
        }
    }
}