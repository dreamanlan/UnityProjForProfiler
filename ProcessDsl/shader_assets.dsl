input("t:shader")
{
	string("filter", "");
	feature("source", "allassets");
	feature("menu", "5.All Assets/Shaders");
	feature("description", "just so so");
}
filter
{
  if(assetpath.Contains(filter)){
  	$v0 = loadasset(assetpath);
  	order = getshaderpropertycount($v0);
  	info = format("{0} property count:{1} texture count:{2}", getfilename(assetpath), order, getshaderpropertycount($v0, 4));
    1;
  }else{
    0;
  };
}
process
{
  $v0 = loadasset(assetpath);
	addshadertocollection($v0);
};