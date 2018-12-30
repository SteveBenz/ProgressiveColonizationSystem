using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public abstract class ModuleTieredSnackProducer
        : TieredResourceCoverter,
          ISnackProducer
    {
		protected override string RequiredCrewTrait => "Scientist";

        public abstract double MaxConsumptionForProducedFood { get; }
	}
}
