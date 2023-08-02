﻿using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("76DB8780-E24A-4166-BD5F-5786AB793856")]

[assembly: AssemblyTitle("Target Planning")]
[assembly: AssemblyDescription("Help for planning future imaging sessions")]
[assembly: AssemblyCompany("Tom Palmer @tcpalmer")]
[assembly: AssemblyProduct("TargetPlanning.NINAPlugin")]
[assembly: AssemblyCopyright("Copyright © 2023")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.2.1.0")]
[assembly: AssemblyFileVersion("1.2.1.0")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "2.0.0.9001")]

[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/tcpalmer/nina.plugin.targetplanning/")]
[assembly: AssemblyMetadata("FeaturedImageURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.targetplanning/main/NINA.Plugin.TargetPlanning/assets/target-planning-logo.png?raw=true")]
[assembly: AssemblyMetadata("ScreenshotURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.targetplanning/main/NINA.Plugin.TargetPlanning/assets/screenshot1.png?raw=true")]
[assembly: AssemblyMetadata("AltScreenshotURL", "https://raw.githubusercontent.com/tcpalmer/nina.plugin.targetplanning/main/NINA.Plugin.TargetPlanning/assets/screenshot2.png?raw=true")]

[assembly: AssemblyMetadata("LongDescription", @"Target Planning is the converse of the NINA Sky Atlas.  Rather than searching for multiple targets for one particular day, Target Planning takes your desired target and shows imaging options across multiple days.  It can also display charts of the entire imaging season as well as annual target and moon altitude.  Various filters let you establish criteria to restrict the available imaging time based on your imaging needs and local circumstances.  Reports can be opened in your browser so you can view, save, print, or convert to PDF.

There are no sequence instructions or other behaviors associated with this plugin.  All interaction takes place on this plugin page.

## Target Options ##
Select a target to get started.  Once you do, the Daily Details, Imaging Season, and Annual Chart buttons will activate.
* Target: select your target.  This behaves identically to the Coordinates Name field in the Framing wizard and supports incremental search.
* RA/Dec: view/change target coordinates.  These fields will reflect the selected Target but also support manual entry.
* Click the button to the right of the target field to send the coordinates to the Framing wizard.  This will work whether they were loaded from the database or manually entered.

## Daily Details Report ##
The Daily Details Report displays a table showing available imaging times and details for each day, starting from Start Date.  The report uses the currently selected target/coordinates as well as the settings for the altitude, time, and moon filter options.  Select a row in the table to see more details for that day.  Click 'HTML Report' in the upper right of the report area to open the report in a browser.

*Notes*
* Some filter settings will simply adjust the available start/end imaging times appropriately, without rejecting the entire day.  Others will reject the day outright.
* Your latitude/longitude are taken from the currently active NINA profile.  If you want to plan for a different location, create a profile for it and make it active.

*Daily Details Report Options*

The following options act as filters for the report so you can tailor the calculation to your needs.  If a filter option includes 'Any', selecting it will disable that filter.

* Start Date: set the start date for the report.
* Days: set the number of days in the report.
* Minimum Altitude: set the minimum altitude that the target must exceed, which restricts the start/end times.  Select 'Above Horizon' to use your custom local horizon.
* Minimum Imaging Time: set the minimum acceptable imaging time.  Days when the available time is below this threshold are rejected.
* Include Twilight: set the type of twilight to include in the available imaging time: None, Astronomical, Nautical, or Civil.
* Meridian Time Span: set the time on either side of the target's meridian crossing that is acceptable for imaging.  The start/end times will be adjusted to reflect this (taking other criteria into account as well).

* Maximum Moon Illumination: set the maximum acceptable moon illumination percentage.  Disabled if Moon Avoidance is used.
* Minimum Moon Separation: set the minimum acceptable angle between the moon and the target.  If Moon Avoidance is enabled, this is the *distance* parameter to the Moon Avoidance formula - see below.
* Moon Avoidance: enable/disable moon avoidance - see below.
* Moon Avoidance Width: set the width parameter for moon avoidance - see below.

## Imaging Season ##
The Imaging Season chart calculates the available imaging hours for the target across a whole year, using all the same filters used for the Daily Details Report.  Click 'HTML Report' in the upper right of the report area to open the report in a browser.  The year used is taken from the Start Date field.  The chart should be approximately centered on the date of maximum altitude at midnight for the year in the Start Date field.  Obviously, any rejected dates will vary by year if you have any of the moon filters enabled.

The upper part plots available imaging hours by date showing time spans that are acceptable or rejected (red).  The lower part plots the moon illumination and the angular separation between moon and target.

Be patient - this can take some time to generate.

## Annual Chart ##
The Annual Chart shows a yearly chart plotting the altitude of your target and the moon at local midnight for each day.  The year used is taken from the Start Date field.

## Moon Avoidance ##
The Moon Avoidance formula (*Moon-Avoidance Lorentzian*) was created by the [Berkeley Automated Imaging Telescope](http://astron.berkeley.edu/~bait/) (BAIT) team.  The formulation used here is from [ACP](http://bobdenny.com/ar/RefDocs/HelpFiles/ACPScheduler81Help/Constraints.htm) and is the same as that used in Dale Ghent's Moon Angle plugin.

The formula progressively relaxes the separation criteria as the moon gets away from full.  It takes two parameters: *distance* (in degrees, from the Minimum Moon Separation field) and *width* (days, from the Moon Avoidance Width field).  Think of distance as the minimum separation you want for a full moon.  Width is then the number of days before or after full to reduce the required separation by half.  When enabled, the separation between the moon and your target must be greater than the value calculated by the formula for that time.

The parameters used on the ACP site (distance = 120, width = 14) are very conservative - especially so for narrowband imaging.  Values like 60/7 may be more applicable.

## Other Notes ##
* Airmass isn't a filter option since it penalizes you for your latitude.  Instead, use the Meridian Time Span filter to select times that minimize airmass for your target.
* The time used for all moon calculations is taken to be the midpoint of the start/end imaging times for that day.  For this reason, the values displayed may differ from what you see in other NINA displays.
* Fixing the moon's position and illumination for the entire night is an obvious approximation.  This may be improved in a future version, possibly also discounting moon impact altogether if it is below the horizon by some distance.
* When using a custom local horizon, be aware that the tool will use the target/horizon crossing closest to the rise or set event for the target (altitude = 0).  For example, if you have a tree in the southwest but then a lower horizon to the west of that, your target may first disappear behind the tree but then reappear later.  The NINA 'Loop while Altitude Above Horizon' instruction would stop you just east of the tree while the plugin wouldn't stop the session until the horizon closest to target setting.
* There is no ability to specify an offset when using a custom horizon.
* If you click 'HTML Report' to open in a browser, you can then save, print, convert to PDF, etc.  Use the available print options to customize the output.

## Acknowledgements ##
* James Lamb for his planning tool ([YouTube](https://www.youtube.com/watch?v=kM8Jy1Kwhr8&t=1s))
* PatriotAstro for pointing me to [airmass.org](https://airmass.org/) and for reviewing an early release
* ACP and BAIT teams for the [Moon avoidance](http://bobdenny.com/ar/RefDocs/HelpFiles/ACPScheduler81Help/Constraints.htm) formula

# Getting Help #
* Ask for help (tag @tcpalmer) in the #plugin-discussions channel on the NINA project [Discord server](https://discord.com/invite/rWRbVbw).
* [Plugin source code](https://github.com/tcpalmer/nina.plugin.targetplanning)
* [Change log](https://github.com/tcpalmer/nina.plugin.targetplanning/blob/main/CHANGELOG.md)

The Target Planning plugin is provided 'as is' under the terms of the [Mozilla Public License 2.0](https://github.com/tcpalmer/nina.plugin.targetplanning/blob/main/LICENSE.txt)
")]

[assembly: ComVisible(false)]
