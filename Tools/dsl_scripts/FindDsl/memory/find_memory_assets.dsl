input
{
    int("maxSize", 8);
    string("category", "native");
    string("class", "Texture2D"){
        native_memory_group;
    };
	stringlist("anyfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "snapshot");
	feature("menu", "6.Memory/slowly find memory assets");
	feature("description", "just so so");
}
filter
{
    String = gettype("System.String");
	order = memory.size;
	if(memory.size >= maxSize && memory.className == class && string_contains_any(memory.name,anyfilter)){
		$v0 = find_asset(memory.name, memory.className);
		asset_path = $v0[0];
		scene_path = $v0[1];
		if(is_null_or_empty(asset_path)){
			asset_path = memory.name;
		};
		info = format("name:{0} class:{1} size:{2} refby:{3}",
            memory.name, memory.className, memory.size, memory.refCount
            );
        value = memory.size;
    	extraobject = memory.objectData;
    	extralistbuild = "BuildExtraList";
        1;
	}else{
        0;
	};
};

script(BuildExtraList)args($item)
{
	$r = find_shortest_path_to_root($item.ExtraObject);
	return($r);
};