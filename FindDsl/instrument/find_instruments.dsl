input
{
	float("minFps", 30);
	float("maxFrameTime", 50);
	float("maxFrameGC", 100);
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
	if(instrument.fps <= minFps || instrument.totalCpuTime >= maxFrameTime || instrument.totalGcMemory >= maxFrameGC){
		value = instrument.totalGcMemory;
		assetpath = "go";
		extraobject = instrument;
		$maxTotalTime = 0;
		$maxTotalTimeName = "[null]";
		$ct = 0;
		extralist = newextralist();
		looplist(instrument.cpuRecords){
			if($ct < 32){
				$record = $$;
				if($record.depth > 0 && ($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC) && (stringcontains($record.name, filterKey) || stringcontains($record.layerPath, filterKey))){
					$name = $record.depth + ":" + $record.name + "|c|" + $record.markerId + "|" + $record.sampleCount;
					extralistadd(extralist, $name, [instrument, $record, instrument.cpuModule]);
					$ct = $ct + 1;

					if($record.totalTime >= $maxTotalTime){
						$maxTotalTime = $record.totalTime;
						$maxTotalTimeName = $name;
					};
				};
			}
			else{
				break;
			};
		};
		looplist(instrument.gpuRecords){
			if($ct < 32){
				$record = $$;
				if($record.depth > 0 && ($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC) && (stringcontains($record.name, filterKey) || stringcontains($record.layerPath, filterKey))){
					$name = $record.depth + ":" + $record.name + "|g|" + $record.markerId + "|" + $record.sampleCount;
					extralistadd(extralist, $name, [instrument, $record, instrument.gpuModule]);
					$ct = $ct + 1;

					if($record.totalTime >= $maxTotalTime){
						$maxTotalTime = $record.totalTime;
						$maxTotalTimeName = $name;
					};
				};
			}
			else{
				break;
			};
		};
		order = $maxTotalTime;
		info = format("frame:{0} count:{1} fps:{2} cpu:{3} gpu:{4} gc:{5} max_total_time:{6} name:{7}",
			instrument.frame, instrument.sampleCount, instrument.fps, instrument.totalCpuTime, instrument.totalGpuTime,
			instrument.totalGcMemory, $maxTotalTime, $maxTotalTimeName
		);
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