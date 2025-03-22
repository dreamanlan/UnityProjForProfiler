input("*.tga","*.png","*.jpg","*.exr")
{
	int("maxSize",256){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneassets");
	feature("menu", "2.Current Scene Resources/Textures");
	feature("description", "just so so");
}
filter
{
	$v0 = loadasset(assetpath);
	if(isnull($v0)){
		0;
	} else {;
		$v1 = $v0.width;
		$v2 = $v0.height;
		$v3 = importer.isReadable;
		$v4 = importer.mipmapEnabled;
		//unloadasset($v0);
		order = $v1 < $v2 ? $v2 : $v1;
		if(($v1 > maxSize || $v2 > maxSize) && assetpath.Contains(filter) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
			info = format("size:{0},{1} readable:{2} mipmap:{3}", $v1, $v2, $v3, $v4);
			1;
		} else {
			0;
		};
	};
};