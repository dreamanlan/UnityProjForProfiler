input("Transform")
{
	stringlist("notfilter", "", "not contains");
	stringlist("tagnotfilter", "", "tag not contains");
	floatlist("maxsize", "500,500,500");
	floatlist("center", "0,0,0");
	float("pathwidth", 240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "4.Current Scene Objects/Add World Box");
	feature("description", "just so so");
}
filter
{
	if(order<1){
		setmaxboundingbox(center[0],center[1],center[2],maxsize[0],maxsize[1],maxsize[2]);
		resetboundingbox();
		selected = true;
		@ct = 0;
		@px = 0;
		@py = 0;
		@pz = 0;
	};
	if(stringnotcontains(scenepath, notfilter) && stringnotcontains(object.tag, tagnotfilter)){
		$tr = object.transform;
		@ct = @ct + 1;
		@px = @px + $tr.position.x;
		@py = @py + $tr.position.y;
		@pz = @pz + $tr.position.z;

		($r, $arr) = mergeboundingbox(object);
		if(!$r){
			$page = listsize(results) / 50.0;
			debuglog("path:{0}, out of bounding box ({1:f2} {2:f2} {3:f2})-({4:f2} {5:f2} {6:f2}), page:{7:f1}", scenepath, $arr[0], $arr[1], $arr[2], $arr[3], $arr[4], $arr[5], $page);
			info = format("path:{0}, out of bounding box ({1:f2} {2:f2} {3:f2})-({4:f2} {5:f2} {6:f2})", scenepath, $arr[0], $arr[1], $arr[2], $arr[3], $arr[4], $arr[5]);
		}
		else {
			$arr = getboundingbox();
			$w = $arr[3] - $arr[0];
			$h = $arr[4] - $arr[1];
			$l = $arr[5] - $arr[2];
			$cx = @px / @ct;
			$cy = @py / @ct;
			$cz = @pz / @ct;
			info = format("path:{0}, merged_bounding_box:({1:f1} {2:f1} {3:f1}) ({4:f2} {5:f2} {6:f2})-({7:f2} {8:f2} {9:f2}) geometry center:({10:f2} {11:f2} {12:f2})", scenepath, $w, $h, $l, $arr[0], $arr[1], $arr[2], $arr[3], $arr[4], $arr[5], $cx, $cy, $cz);
		};
		1;
	}else{
		0;
	};
}
process
{
	addorupdateboundingbox("WorldBoundingBox", "Universal Render Pipeline/Simple Lit", "_BaseColor", (0.5,0.5,0.2,0.5), "_Surface", 1.0, "_Blend", 0.0);
};