input
{
	label("l1","frame's");
	float("minFps", 200, "minFps<=");
	float("maxFrameTime", 1000, "or maxFrameTime>=");
	float("maxFrameGC", 1000, "or maxFrameGC>=");
	label("l2","function's (only the first 64 matching results in unsorted order)");
	float("maxTotalTime", 0, "(maxTotalTime>=");
	float("maxSelfTime", 1000, "or maxSelfTime>=");
	float("maxGC",1000, "or maxGC>=");
	stringlist("containsAny", "", ") and containsAny");
	stringlist("nameNotContains", "", "and nameNotContains");
	int("minDepth", 2, "and minDepth>=");
	inthash("threads", "", "and threads");
	bool("filterPath", false, "filterPath", "If checked, the name or path is involved in the judgment, otherwise only the name is involved in the judgment");
	feature("source", "instruments");
	feature("menu", "7.Profiler/time and gc");
	feature("description", "just so so");
}
filter
{
	if(instrument.fps <= minFps || instrument.totalCpuTime >= maxFrameTime || instrument.totalGcMemory >= maxFrameGC){
		value = instrument.totalGcMemory;
		asset_path = "go";
		extraobject = instrument;
		$maxTotalTimeRecord = null();
		$maxTotalTimeThread = null();
		$maxTotalTime = 0;
		$maxTotalTimeName = "[null]";
		$ct = 0;
		extra_list = new_extra_list();
		loop_list(instrument.records){
			$record = $$;
			if($record.depth >= minDepth && int_hash_contains(threads, $record.threadIndex) && ($record.totalTime >= maxTotalTime || $record.selfTime >= maxSelfTime || $record.gcMemory >= maxGC) && (string_contains_any($record.name, containsAny) || filterPath && string_contains_any($record.layerPath, containsAny)) && string_not_contains($record.name, nameNotContains)){
				$name = $record.depth + ":" + $record.name + "|" + $record.threadIndex + "|" + $record.markerId + "|" + $record.sampleCount;
				if($ct < 64){
					extra_list_add(extra_list, $name, [instrument, $record, instrument.threads[$record.threadIndex]]);
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
		if(!is_null($maxTotalTimeRecord) && !is_null($maxTotalTimeThread)){
			extra_list_add(extra_list, "*" + $maxTotalTimeName, [instrument, $maxTotalTimeRecord, $maxTotalTimeThread]);
		};
		extra_list_add(extra_list, "[goto_frame]", [instrument, null(), null()]);
		extralistclick = "OnClickExtraListItem";
		1;
	}else{
		0;
	};
};

script(OnClickExtraListItem)args($obj,$item)
{
	$instrument = $obj.Value[0];
	$record = $obj.Value[1];
	$thread = $obj.Value[2];
	if(is_null($record)){
    	select_frame($instrument.frame);
	}
	else{
		if($record.threadIndex == 1){
			filtergpu_sample($record.name);
		}
		else{
			filtercpu_sample($record.name, $record.threadIndex);
		};
		select_sample($instrument, $record, $thread);
	};
};