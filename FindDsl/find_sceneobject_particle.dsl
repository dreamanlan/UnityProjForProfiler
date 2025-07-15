input("ParticleSystem")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Particles");
	feature("description", "just so so");
}
filter
{
	$v0 = getcomponent(object,"ParticleSystem");
	if(assetpath.Contains(filter)){
		$v1 = $v0.emission.rateOverTime;
		$v2 = $v0.main.startDelay;
		$v3 = $v0.main.startLifetime;
		info = format("max particles:{0} rate over time:{1}-{2} duration:{3} start delay:{4}-{5} start lifetime:{6}-{7}",$v0.main.maxParticles,$v1.constantMin,$v1.constantMax,$v0.main.duration,$v2.constantMin,$v2.constantMax,$v3.constantMin,$v3.constantMax);
		1;
	}else{
		0;
	};
};