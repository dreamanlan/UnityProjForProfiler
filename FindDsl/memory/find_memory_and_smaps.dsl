input
{
    string("category", "native"){
        popup(["all","native","managed"])
    };
    int("maxSize", 8);
    ulong("sectionAddr",0);
	stringlist("classfilter", "");
	stringlist("filter", "");
	stringlist("classnotfilter", "");
	stringlist("notfilter", "");
	string("mheaps",""){
	    file("csv");
	    script("LoadManagedHeaps");
	};
	string("maps",""){
	    file("txt");
	    script("LoadMaps");
	};
	bool("findasset", false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "snapshot");
	feature("menu", "6.Memory/slowly find memory and smaps");
	feature("description", "just so so");
}
filter
{
	if(memory.size >= maxSize && stringcontains(memory.className, classfilter) && stringcontains(memory.name, filter) && stringnotcontains(memory.className, classnotfilter) && stringnotcontains(memory.name, notfilter)){
		if(findasset){
    		assetpath = (findasset(memory.name, memory.className))[0];
	    }else{
		    assetpath = memory.name;
		};
		if(isnullorempty(assetpath)){
			assetpath = memory.name;
		};
		$v0 = findsmaps(maps, memory.address);
		$v1 = findmanagedheaps(mheaps, memory.address);
		if(isnull($v0)){
		    $v2 = "unknown";
		    $v3 = 0;
		    $v4 = 0;
		    $v5 = 0;
		    $v6 = 0;
		}else{
		    $v2 = $v0.module;
		    $v3 = $v0.size;
		    $v4 = $v0.vm_start;
		    $v5 = $v0.rss;
		    $v6 = $v0.pss;
		};
		if(isnull($v1)){
		    $v7 = 0;
		    $v8 = 0;
		}else{
		    $v7 = $v1.size;
		    $v8 = $v1.vm_start;
		};
		if(sectionAddr==0 || sectionAddr==$v4){
    		scenepath = format("name:{0} class:{1} size:{2} addr:{3:X}",
    	        memory.name, memory.className, memory.size, memory.address
    	        );
    		info = format("module:{0} section_size:{1} section_start:{2:X} rss:{3} pss:{4} mheap_size:{5} mheap:{6:X}",
    	        $v2, $v3, $v4, $v5, $v6, $v7, $v8
    	        );
    	    order = $v4;
    	    value = memory.size;
        	extraobject = memory.objectData;
        	extralistbuild = "BuildExtraList";
    	    1;
    	}else{
    	    0;
    	};
	}else{
	    0;
	};
};

script(LoadManagedHeaps)args($paramInfo)
{
    if(!isnullorempty($paramInfo.Value)){
        $paramInfo.Value = loadmanagedheaps($paramInfo.Value);
        return(3600.0);
    }else{
        return(1.0);
    };
};
script(LoadMaps)args($paramInfo)
{
    if(!isnullorempty($paramInfo.Value)){
        $paramInfo.Value = loadsmaps($paramInfo.Value);
        return(3600.0);
    }else{
        return(1.0);
    };
};
script(BuildExtraList)args($item)
{
	$r = findshortestpathtoroot($item.ExtraObject);
	return($r);
};