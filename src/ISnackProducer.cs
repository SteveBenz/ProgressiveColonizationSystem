using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    internal interface ISnackProducer
		: IProducer
    {
        /// <summary>
        ///   For the type and tier of food, what's the maximum percentage of a kerbal's diet that
        ///   this kind of food can be.
        /// </summary>
        double MaxConsumptionForProducedFood { get; }
    }
}
