input
{
	label("l1","frames");
	float("minFps", 30);
	float("maxFrameTime", 1000);
	float("maxFrameGC", 1000);
	label("l2","functions");
	float("maxTotalTime", 0);
	float("maxSelfTime", 0);
	float("maxGC",0);
	stringlist("containsAny", "");
	stringlist("nameNotContains", "");
	int("minDepth", 1);
	bool("filterPath", false);
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
		$maxTotalTimeRecord = null();
		$maxTotalTimeModule = null();
		$maxTotalTime = 0;
		$maxTotalTimeName = "[null]";
		$ct = 0;
		extralist = newextralist();
		looplist(instrument.cpuRecords){
			$record = $$;
			if($record.depth >= minDepth && ($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC) && (stringcontainsany($record.name, containsAny) || filterPath && stringcontainsany($record.layerPath, containsAny)) && stringnotcontains($record.name, nameNotContains)){
				$name = $record.depth + ":" + $record.name + "|c|" + $record.markerId + "|" + $record.sampleCount;
				if($ct < 32){
					extralistadd(extralist, $name, [instrument, $record, instrument.cpuModule]);
					$ct = $ct + 1;
				};
				if($record.totalTime >= $maxTotalTime){
					$maxTotalTimeRecord = $record;
					$maxTotalTimeModule = instrument.cpuModule;
					$maxTotalTime = $record.totalTime;
					$maxTotalTimeName = $name;
				};
			};
		};
		looplist(instrument.gpuRecords){
			$record = $$;
			if($record.depth >= minDepth && ($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC) && (stringcontainsany($record.name, containsAny) || filterPath && stringcontainsany($record.layerPath, containsAny)) && stringnotcontains($record.name, nameNotContains)){
				$name = $record.depth + ":" + $record.name + "|g|" + $record.markerId + "|" + $record.sampleCount;
				if($ct < 32){
					extralistadd(extralist, $name, [instrument, $record, instrument.gpuModule]);
					$ct = $ct + 1;
				};
				if($record.totalTime >= $maxTotalTime){
					$maxTotalTimeRecord = $record;
					$maxTotalTimeModule = instrument.gpuModule;
					$maxTotalTime = $record.totalTime;
					$maxTotalTimeName = $name;
				};
			};
		};
		order = $maxTotalTime;
		info = format("frame:{0} count:{1} fps:{2} cpu:{3} gpu:{4} gc:{5} max_total_time:{6} name:{7}",
			instrument.frame, instrument.sampleCount, instrument.fps, instrument.totalCpuTime, instrument.totalGpuTime,
			instrument.totalGcMemory, $maxTotalTime, $maxTotalTimeName
		);
		if(!isnull($maxTotalTimeRecord) && !isnull($maxTotalTimeModule)){
			extralistadd(extralist, $name, [instrument, $maxTotalTimeRecord, $maxTotalTimeModule]);
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