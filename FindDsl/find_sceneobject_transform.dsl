input("Transform")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Tranforms");
	feature("description", "just so so");
}
filter
{
	if(order==0){
		resetboundingbox();
	};
	mergeboundingbox(object);
	if(scenepath.Contains(filter)){
		$arr = getboundingbox();
		$w = $arr[3] - $arr[0];
		$h = $arr[4] - $arr[1];
		$l = $arr[5] - $arr[2];
		info = format("path:{0}, merged_bounding_box:({1:f1} {2:f1} {3:f1}) ({4:f2} {5:f2} {6:f2})-({7:f2} {8:f2} {9:f2})", scenepath, $w, $h, $l, $arr[0], $arr[1], $arr[2], $arr[3], $arr[4], $arr[5]);
		1;
	}else{
		0;
	};
};