using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class TieredContainer
        : ITieredContainer
    {
        private PartResource parent;

        public TechTier Tier { get; private set; }

        public TieredResource Content { get; private set; }

        public double Amount
        {
            get => parent.amount;
            set => parent.amount = value;
        }

        public float MaxAmount { get; }

        public static IEnumerable<ITieredContainer> FindAllTieredResourceContainers(IEnumerable<Part> parts)
        {
            foreach (var part in parts)
            {
                foreach (var partResource in part.Resources)
                {
                    if (ColonizationResearchScenario.Instance.TryParseTieredResourceName(partResource.resourceName, out var tieredResource, out var tier))
                    {
                        yield return new TieredContainer
                        {
                            parent = partResource,
                            Tier = tier,
                            Content = tieredResource,
                        };
                    }
                }
            }
        }
    }
}
