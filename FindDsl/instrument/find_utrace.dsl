input
{
	label("l1","frame's");
	float("minFps<=", 30);
	float("or maxFrameTime>=", 1000);
	label("l2","function's");
	float("maxTotalTime>=", 0);
	stringlist("and containsAny", "");
	stringlist("and nameNotContains", "");
	int("and minDepth>=", 1);
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
			if($record.depth >= minDepth && ($record.time >= maxTotalTime) && stringcontainsany($record.name, containsAny) && stringnotcontains($record.name, nameNotContains)){
				$name = $record.depth + ":" + $record.name + "|" + $record.frame + "|" + $record.timelineIndex + "|" + $record.threadId;
				if($ct < 32){
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
			extralistadd(extralist, $name, [utraceframe, $maxTotalTimeRecord]);
		};
		1;
	}else{
		0;
	};
};
