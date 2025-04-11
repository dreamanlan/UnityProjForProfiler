input
{
	label("l1", "frame's");
	float("minFps", 200, "minFps<=");
	float("maxFrameTime", 1000, "or maxFrameTime>=");
	label("l2", "function's (only the first 64 matching results in unsorted order)");
	float("maxTotalTime", 0, "maxTotalTime>=");
	stringlist("containsAny", "", "and containsAny");
	stringlist("nameNotContains", "", "and nameNotContains");
	int("minDepth", 2, "and minDepth>=");
	intlist("threads", "", "and threads");
	feature("source", "utrace");
	feature("menu", "7.Profiler/utrace csv");
	feature("description", "just so so");
}
filter
{
	if(1000.0/utraceframe.time <= minFps || utraceframe.time >= maxFrameTime){
		assetpath = "go";
		extraobject = utraceframe;
		$maxTotalTimeRecord = null();
		$maxTotalTime = 0;
		$maxTotalTimeName = "[null]";
		$ct = 0;
		extralist = newextralist();
		looplist(utraceframe.records){
			$record = $$;
			if($record.depth >= minDepth && threads.IndexOf($record.threadId)>=0 && ($record.time >= maxTotalTime) && stringcontainsany($record.name, containsAny) && stringnotcontains($record.name, nameNotContains)){
				$name = $record.depth + ":" + $record.name + "|" + $record.frame + "|" + $record.timelineIndex + "|" + $record.threadId + "|" + utraceframe.threads[$record.threadId].threadName;
				if($ct < 64){
					extralistadd(extralist, $name, [utraceframe, $record]);
					$ct = $ct + 1;
				};
				if($record.time >= $maxTotalTime){
					$maxTotalTimeRecord = $record;
					$maxTotalTime = $record.time;
					$maxTotalTimeName = $name;
				};
			};
		};
		value = $maxTotalTime;
		order = $maxTotalTime;
		info = format("frame:{0} fps:{1} time:{2} max time:{3} name:{4}",
			utraceframe.frame, 1000.0/utraceframe.time, utraceframe.time, $maxTotalTime, $maxTotalTimeName
		);
		if(!isnull($maxTotalTimeRecord)){
			extralistadd(extralist, "*" + $maxTotalTimeName, [utraceframe, $maxTotalTimeRecord]);
		};
		1;
	}else{
		0;
	};
};
