InProcSerial is a simple supply chain model of a specific shape and size, implemented
	as a straightforward serial Sage model. It is run by setting the project as the
	StartUp project.

InProcParallel is a more complex construct, with several projects - 
	1.) InProcParallelLib : A library of domain objects and some proposed additions to 
		Sage in support of some of the new mechanisms for parallelism.
	2.) InProcParallel : A console app that constructs a model and then runs it in
		accordance with the parameters in TotallyBogusConfigMechanism.cs. This is the
		project you want to run if you want to see parallel behavior.
	3.) InProcParallelTest : A test library with some early attempts at tests - note that
		they're more like "exercisers of the mechanism" than tests, at this point, and
		they've got embarrassingly low coverage, too. Sue me. :-)

Both InProcSerial and InProcParallel have their own README files, describing the models
	in somewhat more detail.