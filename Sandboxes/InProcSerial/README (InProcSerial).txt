This project runs, in a standard serial Sage model, a simulation of a three-tier Supply Chain.

The first tier is a single Market Segment which generates orders, one SKU each, and throws them
	at randomly selected Fulfillment Centers. It also receives fulfilled orders from those FC's.
The second tier is a number of Fulfillment Centers that receive and fulfill orders from the Market
	Segment, satisfy those orders, and at a single specified safety level, orders replenishments.
The third tier is a single Replenisher (in this case, a factory) that fulfills orders from the
	Fulfillment Centers.

When the simulation completes, it creates and writes a summary Excel file to the run directory 
	(i.e. the directory from which the executable was run.) Note that it does not actually LAUNCH
	excel.

Many characteristics of the simulation can be adjusted by editing the "config.XML" file in the
	run directory. NOTE: If "config.xml" doesn't exist, it is created the first time the executable
	is run, and	may, thereafter, be edited for further runs. Read ModelConstants.cs for explanations
	of what the settings do.
