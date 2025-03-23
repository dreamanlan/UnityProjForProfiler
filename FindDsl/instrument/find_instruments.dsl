input
{
  float("minFps", 30);
  float("maxTime", 50);
  float("maxGC", 100);
	feature("source", "instruments");
	feature("menu", "7.Profiler/time and gc");
	feature("description", "just so so");
}
filter
{
	order = instrument.index;
	if(instrument.fps <= minFps || instrument.totalCpuTime>=maxTime || instrument.totalGcMemory>=maxGC){
	  info = format("frame:{0} fps:{1} cpu:{2} gpu:{3} gc:{4}",
	  		instrument.frame, instrument.fps, instrument.totalCpuTime, instrument.totalGpuTime, instrument.totalGcMemory
	    );
	  value = instrument.totalGcMemory;
	  assetpath = "go";
	  extraobject = instrument;
	  extralist = newextralist("[goto_frame]" => instrument.frame);
	  extralistclick = "OnClickExtraListItem";
	  1;
	}else{
	  0;
	};
};

script(OnClickExtraListItem)args($obj,$item)
{
    selectframe($obj.Value);
};