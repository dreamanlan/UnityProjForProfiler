input
{
	float("minFps", 30);
	float("maxTime", 50);
	float("maxTotalGC", 100);
	float("maxTotalTime", 10);
	float("maxSelfTime", 1);
	float("maxGC",10);
	string("filterKey", "");
	feature("source", "instruments");
	feature("menu", "7.Profiler/time and gc");
	feature("description", "just so so");
}
filter
{
	order = instrument.index;
	if(instrument.fps <= minFps || instrument.totalCpuTime >= maxTime || instrument.totalGcMemory >= maxTotalGC){
		info = format("frame:{0} fps:{1} cpu:{2} gpu:{3} gc:{4}",
			instrument.frame, instrument.fps, instrument.totalCpuTime, instrument.totalGpuTime, instrument.totalGcMemory
		);
		value = instrument.totalGcMemory;
		assetpath = "go";
		extraobject = instrument;
		$ct = 0;
		extralist = newextralist();
		looplist(instrument.cpuRecords){
			if($ct < 32){
				$record = $$;
				if($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC || stringcontains($record.name, filterKey) || stringcontains($record.layerPath, filterKey)){
					extralistadd(extralist, $record.name, [instrument, $record, instrument.cpuModule]);
				}
			}
			else{
				break;
			};
			$ct = $ct + 1;
		};
		looplist(instrument.gpuRecords){
			if($ct < 32){
				$record = $$;
				if($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC || stringcontains($record.name, filterKey) || stringcontains($record.layerPath, filterKey)){
					extralistadd(extralist, $record.name, [instrument, $record, instrument.gpuModule]);
				}
			}
			else{
				break;
			};
			$ct = $ct + 1;
		};
		extralistadd(extralist, "[goto_frame]", [instrument, null(), null()]);
		extralistclick = "OnClickExtraListItem";
		1;
	}else{
		0;
	};
};

script(OnClickExtraListItem)args($obj,$item)
{
	if(isnull($obj.Value[1])){
    	selectframe($obj.Value[0].frame);
	}
	else{
		selectsample($obj.Value[0], $obj.Value[1], $obj.Value[2]);
	};
};