input("*.prefab")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Missing Script");
	feature("description", "just so so");
}
filter
{
	if(assetpath.Contains(filter)){
    $v0 = loadasset(assetpath);
	  $v1 = getcomponentsinchildren($v0, "Component");
	  $v2 = 0;
	  looplist($v1){
	    if(isnull($$)){
	      $v2=1;
	    };
	  };
	  $v2;
	}else{
	  0;
	};
};