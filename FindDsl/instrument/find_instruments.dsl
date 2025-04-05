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
		$maxTotalTimeThread = null();
		$maxTotalTime = 0;
		$maxTotalTimeName = "[null]";
		$ct = 0;
		extralist = newextralist();
		looplist(instrument.records){
			$record = $$;
			if($record.depth >= minDepth && ($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC) && (stringcontainsany($record.name, containsAny) || filterPath && stringcontainsany($record.layerPath, containsAny)) && stringnotcontains($record.name, nameNotContains)){
				$name = $record.depth + ":" + $record.name + "|" + $record.threadIndex + "|" + $record.markerId + "|" + $record.sampleCount;
				if($ct < 32){
					extralistadd(extralist, $name, [instrument, $record, instrument.threads[$record.threadIndex]]);
					$ct = $ct + 1;
				};
				if($record.totalTime >= $maxTotalTime){
					$maxTotalTimeRecord = $record;
					$maxTotalTimeThread = instrument.threads[$record.threadIndex];
					$maxTotalTime = $record.totalTime;
					$maxTotalTimeName = $name;
				};
			};
		};
		order = $maxTotalTime;
		info = format("frame:{0} fps:{1} cpu:{2} gpu:{3} gc:{4} max_total_time:{5} name:{6}",
			instrument.frame, instrument.fps, instrument.totalCpuTime, instrument.totalGpuTime,
			instrument.totalGcMemory, $maxTotalTime, $maxTotalTimeName
		);
		if(!isnull($maxTotalTimeRecord) && !isnull($maxTotalTimeThread)){
			extralistadd(extralist, $name, [instrument, $maxTotalTimeRecord, $maxTotalTimeThread]);
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