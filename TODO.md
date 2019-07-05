# Progressive Colonization System Work List

Because I can't be bothered with GitHub issues.

1. Bugs:
   1.  Resource transfer seems not to work for some ships -- perhaps because of staging
   2.  (fixed) Can bring CrushIns from Kerbin or other bodies and fake out the resource auto-collection mechanic.  It should be happy
       to take the crushins, but disallow it as an auto-collector.
   3.  (fixed) If there's more than one manned base, it should ask you where you want to look for crushins
   4.  (no-repro) Something seems to reset the dialog box to center from time to time
   5.  (fixed) Maybe it's not consuming the crushins out of the ground.
   6.  (fixed) K&K Workshop part is not in the PKS tab
   7.  (fixed) Messy text when kerbals run out of food
   8.  (fixed) No message outside of cupcake dialog when kerbals first run out of food
   9.  (fixed) Doesn't tell you that your ship has no food when you have parts that require kerbals and no kerbals
   10. (fixed) If you just set the body without setting the tier, it sets the parts to Tier 4
   11. (fixed) Crew dialog is busted in the SPH -- thinks all the parts are disabled and doesn't show any crew
   12. (fixed) Complains about a lack of fertilizer when T4 fertilizer is present and there's a Hydroponics producer
   13. (fixed) Crew dialog does not update when kerbals are added in the Editor
   14. (fixed) On Progression pane, "Production" comes up twice at Mun Base 1
   15. (fixed) Tooltips apparently don't get line-wrapped.
   16. (fixed) Scan for Loose Crushins shouldn't be visible below T2
   17. (fixed) If there's no Complex Parts, it doesn't list that as a limiter in the in-flight production tab. (nocompwrng)
   18. (fixed) If there's no complex parts, it doesn't list that as a problem in the editor warnings tab.
       ^-- both of these might be due to it not showing any indicator if you don't have storage for the output. <<-- true dat
   19. (fixed) Stuff Scanner part is showing up under Science tab
   20. (fixed) If a Rocket part can't produce because of no storage, it doesn't get shown
   21. (fixed) Transfer sending & receiving T4 snacks  (probably should just never send T4 snacks to a base that
       can produce it) (t4snackbug)
   22. (fixed) If a kerban runs out of food, it continuously spans "Valentina don't has have enough to eat!  ..."
       (Note the outstanding grammar.)  Still broke - (foodspam) - regression was only in the case where more than
       one vessel was in the scene.
   23. (fixed) Sometimes it doesn't detect that you have scansats
   24. (fixed) Looks like if it runs out of food, it makes the kerbals complain, but only start to think about going tourist
       at warp-in time
   25. (fixed) If a thing can't be produced (either because of lack of input or output space), then it just doesn't show up
       in the progress tab - it should say "production blocked" or something. (nocompwrng - then add rocket part
       storage on gilly t1 base)
   26. (fixed) Could stand to have a 'refresh' button in the Transfer window.
   27. When at a base with disabled parts and parts that are limited by not being at the final tier, it says
       research is limited by the disabled parts
   28. When research is limited by "not cutting edge parts", it's a bit overblown.  Maybe "Tier-4 research complete"
       in a gentler color.
2. Rounding things out
   1.  Create or hijack a part for storing Snacks - just snacks
   2.  (complete) Write PDF help and or create a github wiki
   3.  (complete) Make the shinies boost your rep on landing - remember to add checks to make sure Shinies don't actually come from Kerbin
   4.  Get a better scanner part
   5.  Fully automate release preparation
   6.  Add .version to output per guidance: https://github.com/KSP-CKAN/NetKAN/pull/7147
       ^-- added the version file, but CKAN doesn't know about it yet.  Fix it at the next version update.
   7.  Maybe allow some warnings to show a "Don't care" button (Particularly the storage ones)
   8.  (DOA) Make a "K&K Storage [KIS]" that works with B9 -- need to figure out how to do the decals and push it to K&K
       Mod author has other ideas.
3. Things that don't seem worth doing
   1.  Integrate with Kerbal Alarm Clock?
   2.  IDEA: Make it so that having achieved Tier-X farming on one world makes Tier-X-1 easier to achieve on a new world.
   3.  Delete the parts supporting EL's production chain
     
