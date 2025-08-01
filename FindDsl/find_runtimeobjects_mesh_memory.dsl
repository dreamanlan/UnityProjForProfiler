input("Mesh")
{
	int("maxSize",10){
		range(1,1024);
	};
	float("pathwidth",240){range(20,4096);};
	feature("source", "runtimeobjects");
	feature("menu", "3.Current Scene Objects/Mesh Memory");
	feature("description", "just so so");
}
filter
{
	$v0 = object;
	if(isnull($v0)){
		$r = 0;
	} else {
		$v1 = $v0.name;
		$v2 = getruntimememory($v0);
		order = $v2;
		value = $v2/1024.0/1024.0;
		if($v2 >= maxSize * 1024.0){
			info = format("name:{0} runtime memory:{1:f3}mb", $v1, $v2/1024.0/1024.0);
			$r = 1;
		} else {
			$r = 0;
		};
	};
	$r;
};