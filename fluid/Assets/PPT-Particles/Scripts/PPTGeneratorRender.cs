using UnityEngine;
using System.Collections;
using System.Threading;
using PPTParticleSystem2D;

//Generator for the PPT Particle System
public class PPTGeneratorRender : MonoBehaviour
{
	//Core of the PPT Particle System
	private PPTParticleSystem physics;
	
	//Texture to make as a particle system
	//Alpha Channel supported (each alpha = 0 means no particle)
	private  Texture2D image;
	public RenderTexture tex;
	
	//Final ressolution for the particle system
	//Small value mean more resolution
	[Range(0.1f, 100)]
	public float resolution = 6;
	
	//A minium and maxim value for a random mass for the particles
	public float minimMassValue = 0.4f;
	public float maxMassValue = 0.8f;
	
	//Values  for the spring and damping particles system
	public float springConstant=0.02f, damping=0.04f;

	
	//Particle Sistem from unity used to make the render of the particles
	public ParticleSystem ParticleSystem;
	
	//External forces applyied to the system
	public PPTForceParticle[] externalForces;
	
	//Use or not the localTransformPosition from the external forces
	public bool localPosition = false;
	
	//Particle method system to calculate the steps
	public PPTParticleSystem.IntegratorSystem integratorSystem;
	
	//Bolean flag to know if the particle system is loaded
	public bool isLoaded;
	
	//Value to make more viscosity effect
	[Range(0, 0.7f)]
	public float somedrag;
	
	//Enable or disable the srpings system
	public bool springsActivated = true;

	//the size of the representation of the system
	public float size = 1;
	
	//Internal variables for the map of the texture.
	private int widthSmall, heightSmall, numPixelsSmall;
	private PPTParticle[] particles;
	private PPTSpring[] springs;
	private PPTParticle[] fixedParticles;
	private Color[] colors;
	private float[] forceInstance;
	private bool[] activeInstance;
	private PPTParticle[] instances;
	private PPTAttraction[,] attracts;
	private UnityEngine.ParticleSystem.Particle[] parts;
	private bool m_springsActivated=true;
	
	//Multi thread for optimitzation
	private Thread thread;
	private Mutex mainLoop;
	
	void OnApplicationQuit()
	{
		//Kill the thread from the PPTParticleSystem
		thread.Abort();
	}
	
	void Start()
	{
		Camera.main.gameObject.AddComponent<CameraRender> ();
		Camera.main.gameObject.GetComponent<CameraRender> ().script = this;

		image=new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
		
		RenderTexture.active = tex;
		image.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		image.Apply();
		
		//Start the loading for the PPTParticleSystem
		LoadParticles();
	}
	
	// Use this for initialization
	public void LoadParticles()
	{
		isLoaded = false;
		//get the final ressolution for the system
		widthSmall = (int)(image.width / resolution);
		heightSmall = (int)(image.height / resolution);
		
		numPixelsSmall = 0;
		
		for (int x = 0; x < widthSmall; x++)
		{           
			for (int y = 0; y < heightSmall; y++)
			{        
				//Count each particle from the alpha cut off
	//			Color c = image.GetPixel((int)(x * resolution), (int)(y * resolution));
				numPixelsSmall++;
			}
		}
		

		//Define an array for the colors of the image
		colors = new Color[numPixelsSmall];
		
		//Define a PPTParticle system
		physics = new PPTParticleSystem(0f, somedrag);
		
		//Define the integrator system
		physics.setIntegrator(integratorSystem);
		
		//Define one particle array system
		particles = new PPTParticle[numPixelsSmall];
		
		//Define another array of the particle system for the static position
		fixedParticles = new PPTParticle[numPixelsSmall];
		
		//Define each spring for each particle
		springs = new PPTSpring[numPixelsSmall];
		
		//Define each attract for the external force
		attracts = new PPTAttraction[externalForces.Length, numPixelsSmall];
		
		//Define each external force instance
		instances = new PPTParticle[externalForces.Length];
		forceInstance = new float[externalForces.Length];
		activeInstance = new bool[externalForces.Length];
		
		//For each external force, make a fixed particle
		Vector3 v = new Vector3(0, 0, 0);
		for (int i = 0; i < externalForces.Length; i++)
		{
			if (localPosition)
				v = externalForces[i].transform.localPosition;
			else
				v = externalForces[i].transform.position;
			
			//Make a fixed particle 
			instances[i] = physics.makeParticle(1, v.x, v.y, v.z);
			instances[i].makeFixed();
			forceInstance[i] = externalForces[i].forceParticle;
			activeInstance[i] = externalForces[i].gameObject.activeSelf;
		}
		
		//Now we work with the particle's system
		int a = 0;
		for (int x = 0; x < widthSmall; x++)
		{           
			for (int y = 0; y < heightSmall; y++)
			{        
				//get the current color of the image
				Color c = image.GetPixel((int)(x * resolution), (int)(y * resolution));

				//get the current color 
				colors[a] = image.GetPixel((int)(x * resolution), (int)(y * resolution));
					
				//make a particle
				particles[a] = physics.makeParticle(
					Random.Range(minimMassValue, maxMassValue),
					(x * resolution - widthSmall * resolution / 2)/(60/size),
					(y * resolution - heightSmall * resolution / 2)/(60/size), 0);

				//make a static particle
				fixedParticles[a] = physics.makeParticle(
					Random.Range(minimMassValue, maxMassValue),
					(x * resolution - widthSmall * resolution / 2)/(60/size),
					(y * resolution - heightSmall * resolution / 2)/(60/size), 0);
					
				//active the fixed particle for the static particle
				fixedParticles[a].makeFixed();
					
				int i = 0;
				//for each force...
				foreach (PPTParticle p in instances)
				{
					//apply the attracttion for each external force
					attracts[i, a] = physics.makeAttraction(particles[a], p, forceInstance[i], 0.1f);
					if (!activeInstance[i])
						//turn off the current attractot if the external force isn't enabled
						attracts[i, a].turnOff();
					i++;
				}
					
				//Finally make the spring joint between particle and fixed particle
				springs[a] = physics.makeSpring(particles[a], fixedParticles[a], springConstant, damping, 0);
				a++;
			}
		}
		//Particle system loaded
		isLoaded = true;
		
		//Internal array of particles to work with the Unityt particle system
		parts = new ParticleSystem.Particle[numPixelsSmall];
		
		mainLoop = new Mutex(true);
		thread = new Thread(runPhysics);
		
		//Atart a thread to make the particle system run 
		thread.Start();
		
		//Getting the actual Unity particle system to make the new one
		
		if (ParticleSystem != null)
		{
			float particleSize = ParticleSystem.startSize;
			
			int i = numPixelsSmall;
			while (--i > -1)
			{
				ParticleSystem.Particle particle = new ParticleSystem.Particle();
				particle.position = new Vector2(particles[i].position.x,particles[i].position.y);
				particle.startLifetime = float.MaxValue;
				particle.remainingLifetime = float.MaxValue;
				particle.size = particleSize;
				particle.color = colors[i];
				parts[i] = particle;
			}
			ParticleSystem.SetParticles(parts, parts.Length);
			ParticleSystem.Play();
		}
		else {
			Debug.LogError("Particle system not setted! Please attach one Unity particle system to the PPTParticleSystem.");    
		}		
	}
	
	bool hold=false;


	public void OnPostRender() {
		hold=false;
		RenderTexture.active = tex;
		image.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		image.Apply();
		hold = true;
	}
	
	void Update()
	{
		//For each force, update the state, force and position
		updateExternalForces();
		
		//Make the update of the particle system
		if (physics != null)
			updateParticleSystem();
		
		//Update the spring relations into the PPT Particle System
		updateSpringSystem();
		
		mainLoop.ReleaseMutex();
		mainLoop.WaitOne();
	}
	
	private void updateSpringSystem()
	{
		//Reload the spring state
		if (m_springsActivated != springsActivated)
		{
			m_springsActivated = springsActivated;
			foreach (PPTSpring s in springs)
				if (m_springsActivated)
					s.turnOn();
			else
				s.turnOff();
		}
	}

	//we work with the external forces that will reppel the particles
	private void updateExternalForces()
	{
		Vector3 v = new Vector3(0, 0, 0);
		for (int i = 0; i < externalForces.Length; i++)
		{
			if (localPosition)
				v = externalForces[i].transform.localPosition;
			else
				v = externalForces[i].transform.position;
			
			//Update position for each external force
			instances[i].position.x = v.x;
			instances[i].position.y = v.y;
			instances[i].position.z = v.z;

			
			if (forceInstance[i] != externalForces[i].forceParticle || activeInstance[i] != externalForces[i].gameObject.activeSelf)
			{
				forceInstance[i] = externalForces[i].forceParticle;
				activeInstance[i] = externalForces[i].gameObject.activeSelf;
				
				if (activeInstance[i])
					for (int a = 0; a < numPixelsSmall; a++)
				{
					//Turn on and setup the force for the external force
					attracts[i, a].turnOn();
					attracts[i, a].setStrength(forceInstance[i]);
				}
				else
					for (int a = 0; a < numPixelsSmall; a++)
						//Turn off the external force
						attracts[i, a].turnOff();
			}
		}
	}
	
	public void runPhysics()
	{
		while (true)
		{
			//Thread to run the core of the PPT Particle System
			Thread.Sleep(0);
			physics.tick();
			mainLoop.WaitOne();
			mainLoop.ReleaseMutex();
		}
	}
	
	public void updateParticleSystem()
	{
		
		//Get the current array of particles
		ParticleSystem.GetParticles(parts);
		
		
		//For each particle of Unity particle system, update the position and life
		int i = Mathf.Min(numPixelsSmall, particles.Length); 
		
		int currentPixelX = widthSmall;
		int currentPixelY = heightSmall;
		
		//we update the color each frame in order to represent the texture
		while (--i > -1)
		{

			parts[i].position = new Vector2(particles[i].position.x, particles[i].position.y);
			parts[i].remainingLifetime = float.MaxValue;
			parts[i].color = image.GetPixel((int)(currentPixelX*resolution), (int)((currentPixelY)*resolution));

			
			if (currentPixelY > 1){
				if (currentPixelY == heightSmall){
					currentPixelX--;
				}
				currentPixelY--;
				
			}else{
				currentPixelY = heightSmall;
				parts[i].color = new Color(0,0,0,0);

			}

		
		}
		//Attach the new array to the current Unity particle system
		ParticleSystem.SetParticles(parts, particles.Length);

	}
	
}
