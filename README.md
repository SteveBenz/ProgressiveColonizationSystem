ColonizationByNerm

See the forum thread for the full manifesto.



Development plan:

1. (complete)  Hello World - figure out how to get a mod set up & compiled
2. (complete)  Minimum playability - the ability to launch Kerbals into space that eat food and complain when they don't get it.
   1.  (complete) The ability to research some new parts that do agriponics.
   2.  (complete) The ability to create a station in kerban's SOI and rank up the "Tiers" of agriponics
   3.  (complete) The ability for Kerbals to run out of supply and get grumpy (turn into Tourists)
   4.  (complete) Grumpy Kerbals will get happy again upon return to Kerban
   5.  (sortakinda) Need appropriate crew to do agroponics and research
   6.  (complete) Can't do research on Agriponics T3 in Kerbin's SoI
3. (complete)Basic Progression - the ability to plan a mission to Duna
   1.  (complete) Kerbals can do agriculture on the Mun or Minmus
       1.  (complete) Adapt an Agriculture part from USI
       2.  (complete) Extend the research model to Agriculture & update the editor to show it
       3.  (complete) Can set target body in Editors for an agriculture part
   2.  (complete) Can change resource types for canisters
   3.  (complete) Can manufacture T0 fertilizer
4. Basic Progression - the execution of a mission to Duna
   1.  (complete) Editors can show good data to guide the construction of ships (see GetInfo())
   2.  Tweak the mass and density of Fertilizer & Supplies
   3.  (complete)Agroponic and Ag modules consume some electricity
   4.  Actually enforce the crew counts, types & stars (see 2.v)
   5.  Make the reporting of when you make a tier breakthrough more informative and fun
   6.  IDEA: Make it so that having achieved Tier-X farming on one world makes Tier-X-1 easier to achieve on a new world.
5. Release prep
   1.  Figure out how to Release
   2.  Release to CKAN
6. Advanced Progression - the ability to have extended missions on Duna
   1.  Create the resource gathering mechanic
       1.  Create a configurable part for storing Stuff
       2.  (complete)Create a KSP Resource for Tiered Stuff resource
       3.  (complete)Create a scanning lab (using skin from infrared telescope?)
       4.  Figure out how to make magic spots and waypoints to them
       5.  Make the Tier-1+ drill require being at a magic spot to work
       6.  Actually require that a scanner of the appriate tier be in orbit
       7.  Make the scanner require a network of scanner satelites
   2.  Create shinies resource chain
       1.  Create shinies factory
       2.  Create shinies containers that don't let you fill them at the KSP -- idea: Mun/Minmus shinies peak at T1, Duna/Ike/Gilly at T2, Dres/Eloo/Moho at T5
       3.  Make the shinies boost your rep
   3.  Support USI kerbal types
   4.  Life support dialog works nicely in the editors - can estimate usage and ag production
   5.  (complete) Make the GetInfo()'s show the electric utilization
7. Play Nice With Others
   1.  (cut - sticking with the game one) Integrate with Toolbar?
   2.  Integrate with ModuleManager
   3.  Integrate with Community Resource Kit?
   4.  Integrate with Community Categories?
   5.  Integrate with Kerbal Alarm Clock?
   6.  Integrate with the other base parts mod whose name I forget?
   7.  Either reskin the stolen parts or depend on their sources and re-configure them
   8.  Write PDF help

Known Issues:
   1.  If you create a brand new vessel and add a Snack or Fertilizer container to it as the first part, it initially won't
       show any supplies in it.  Clicking "Next Tier" fixes it.  (Looks to me like a bug in KSP - forum thread:
       https://forum.kerbalspaceprogram.com/index.php?/topic/181234-partmoduleoninitialized-not-being-called-on-first-part/)'