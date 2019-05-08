# Progressive Colonization System

This mod extends Kerbal Space Program with a simple life support mechanism and a production model.  The mod is
different from previous mods along the same vein in that it requires very little math to figure out what
your production rates will be, and it requires incremental exploration.

[See the forum thread](https://forum.kerbalspaceprogram.com/index.php?/topic/181852-15x~16x-progressivecolonizationsystem-life-support-colonization-for-more-fun-in-late-game/)
for the full story.

# TODO

1. Bugs:
   1.  Something seems to reset the dialog box to center from time to time
   2.  (fixed) Once you transfer something, you can't switch to a different transfer target
   3.  (fixed) Cupcake not available in map mode
   4.  (fixed) Cupcake disabled for ships carrying tiered stuff - made it available for landed ships or crewed ships
   5.  (fixed) Fertilizer really is too expensive
   6.  (fixed) Hydroponics works at the launchpad
   7.  On worlds where Shinies production is capped, the max-tier warning ("All production parts should be set to") incorrectly wants to set the level to the capped value
   8.  All the parts that are landed should be touched by the max scanning level warning
   9.  (fixed) Production dialog doesn't tell you that it's lacking CrushIns
   10. (fixed) Production dialog should show excess production capacity for stuff & fertilizer
   11. If there's more than one manned base, it should ask you where you want to look for crushins
   12. Resource transfer seems not to work for some ships -- perhaps because of staging
   13. (fixed) Dialog should show Crushin usage when the rover isn't automated (showcrushin)
   14. (fixed) Production dialog shows LooseCrushins for rover (noloose)
   15. (fixed) Doesn't tell you that it used up all the local loose crushins
   16. (fixed) Doesn't tell you that production is blocked by lack of crew (shortoncrew)
   17. Revisit the SPH production dialog
   18. Didn't get warning for not being maxtier on Minmus T4 -- perhaps because maxtier is 4?
   19. Find a way to automatically set body & tier
   20. Can bring CrushIns from Kerbin or other bodies and fake out the resource auto-collection mechanic.  It should be happy
       to take the crushins, but disallow it as an auto-collector.
   21. Progression doesn't show why research is disabled.
2. Rounding things out
   1.  Write PDF help and or create a github wiki
   2.  (done) Make a K&K drill (Reuse "K&K Metal Ore Drill")
   3.  (in test) Make a "K&K Storage [KIS]" that works with B9 -- need to figure out how to do the decals and push it to K&K
   4.  (done) De-fugly the part tweakable displays
   5.  Fully automate release preparation
   6.  Create or hijack a part for storing Snacks - just snacks
   7.  Rearrange the parts VAB tabs so there's not a pile of random in "Life Support"
   8.  Get a better scanner part
   9.  (completish)Delete the Community Resource Kit storage options on b9 tanks
         Maybe we should use the existance of the simple
   10. Make the shinies boost your rep on landing - remember to add checks to make sure Shinies don't actually come from Kerbin
   11. Add .version to output per guidance: https://github.com/KSP-CKAN/NetKAN/pull/7147
       ^-- added the version file, but CKAN doesn't know about it yet.  Fix it at the next version update.
3. Things that don't seem worth doing
   1.  Integrate with Kerbal Alarm Clock?
   2.  IDEA: Make it so that having achieved Tier-X farming on one world makes Tier-X-1 easier to achieve on a new world.
   3.  Delete the parts supporting EL's production chain
     

# Rambling About The Numbers

There are a number of bits that need to be tweaked with care.  The following or more or less
a justification of the current state of affairs.

When looking at resources, there's an arbitrary "Unit" - which is a number that's just made up.
Each resource also has a density, which is defined in Resources.cfg, and a unitsPerVolume, that
is used by B9 in TankTypes (PKS-B9TankTypes.cfg).  The other numbers to think about are the crew
requirements (in the PksRequiredCrew module descriptors) and the capacity of the part (in
PksTieredResourceConverter).

A "Unit" is a completely arbitrary unit of measure, and so it's chosen to be the amount of snacks
a Kerbal will consume in a day so that players can get easily eyeball how much stuff to bring.
Similarly, a "Unit" of "Fertilizer" is enough fertilizer to make one unit of snacks.

For Hydroponics, the idea is that the Kerbals' waste is being fed back into the system and
so fertilizer, whatever it is, weighs a small fraction of what a snack does, unit-for-unit.

So, let's make up some numbers:
   
1.  A kerbal consumes 2kg (roughly 4.4 pounds) of snacks per day.
2.  The KRDA-O200 Cargo Container is about as big as maybe 1.5 Mk1 command pods, and I'm
    eyeballing it as being able to hold about 95 days worth of food.  That puts its mass
    at 190kg or .2t.  By comparison, the FL-T200 Fuel Tank is roughly the same volume,
    and it carries 1t of fuel.  That seems a bit awkward at first, but LO2 is a little
    bit denser than water, which is plenty dense, so the number seems workable.
3.  The KRDA-O200 Cargo Container has a base volume of 950 units (which look to be about
    a liter which is 1/1000 of a cubic meter).   If we want to have it fit 95 units,
    that means we need 10 units of volume (liters) for each snack.  That means the
    B9_TANK_TYPE.Resource[name=Snacks*].unitsPerVolume needs to be .1.  The 10liters
    per day thing seems a bit steep, but really the problem is mainly that the cargo
    container would need to have a 
4. As stated, density, RESOURCE_DEFINITION[name=Snacks*].density, is in tons/unit, so
    it's just 2kg*.001ton/kg or .002t/unit

And if we ballpark fertilizer as being a tenth the mass of a snack, and, for entertainments
sake, twice as dense, then its "density" is .0002t/unit and its unitsPerVolume is 10*2*.1 = 2.

The density of the supplies is a bit low, but I think the fix, if one were to make it would
be to modify the tank so that it's got less interior space (insulation, e.g.).  I don't
know for sure how to do that with tank configurations today, but perhaps someday...

Now, as to the labor:  It seems like it ought to take no more than 1 kerbal in 4 to produce
the food.  Also, just by eyeball, the parts that we get in StationPartsExpansionRedux all
look to be about the same size and look to me like it ought to support 2 kerbals each.
So, on the surface of things, that'd seem like we should put the "capacity" of those parts
at 2, and the crew requirement at .5.

Practically speaking, the way the math works out, at early tiers the module can support
almost 5x as many kerbals as it does at the max tier, because each kerbal eats way less of
the produce.

At this point, the best answer seems to just let that ride, even if it does mean that parts
become more labor-intensive as they mature.

For farming, we want to make it about twice as productive so that the crew can produce enough
food while landed to take with them on the return trip.  The planetary greenhouse that comes
from PlanetaryBaseInc looks the part, but it's kindof small looking, so it gets the same
productivity as the greenhouse parts but takes half the manpower.

The drill is kindof a strange egg for the base because in mid-to-late game, we want it to be
sortof a magical part because we want a very small number of kerbals to go out in a rover and
pick some stuff up without having to make a big annoying multi-day time-warp with all the
hassles of the day-night-cycle electrics in KSP.  It would also be kindof nutty if the mass
of the mined stuff was less than the mass of the final product.  Yet we don't want to have
to have hugely massive loads of ore traveling around either.

Best way forward I can see is that there's basic dirt that gets mined up around the base and
special sauce that you go and get.  If you go that way, then you don't need to store what the
drill produces at all, and so you don't have to care what its mass & density are.

For the Shinies factory, it doesn't matter at all what number we choose since we can fudge
for whatever mass and value later.  Since the numbers are so arbitrary, we might as well say
that the productivity of shinies factories equals fertilizer factories.  For no good reason,
we can declare the shines to have half the volume and twice the density of snacks.  Value is
the more important measure, but still, there's no real money problem in KSP, so we might as
well be generous.  The average ship costs ~120000 credits, and if you made an effort to
create shinies at all, you could make 1200 in a mission, so we could call T0 100 cash per
shiny at Tier-0 and ramp it up by doubling it at each tier.  If you take the time to haul a mess
of T5 shinies, you deserve to get paid, I suppose.

