using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Star : MonoBehaviour, IInspectable
{
	private bool _selected = false;
	private GameObject _infoPanel;
    public List<Planet> planets = new List<Planet>();
	public JobBoard jobBoard;

    // Prefab templates
	public GameObject planetTemplate;
	public GameObject shipTemplate;
	public GameObject storageNodeTemplate;
	public GameObject[] resourceNodeTemplates = { };
	public GameObject[] factoryTemplates = { };
	public GameObject companyTemplate;

	public Company transportCompany; // TODO: don't store this in the star...
	public Company productionCompany;

	[Range(1f, 3000f)]
	public float planetSeperationDistance = 50;
    [Range(0, 1000)]
    public int planetSpawnMin = 0;
    [Range(0, 1000)]
	public int planetSpawnMax = 10;
	[Range(1f, 100f)]
	public float moonDistance = 7f;
	[Range(0, 500)]
	public int numOfShips = 20;

    private AssetLoader _assets;

    private string[] _starNames = 
    {
		"Archimedes",
		"Plato",
		"Socrates",
		"Pasteur",
		"Galileo",
		"Kepler",
		"Gauss",
		"Curie",
		"Joule",
		"Olympus",
		"Hades",
		"Copernicus",
		"Descartes",
		"Pericles"
	};

	public void Start()
    {
        // Get references to all assets.
        _assets = FindObjectOfType<AssetLoader>();

        // Add a new job board to the star system
        jobBoard = gameObject.AddComponent<JobBoard> ();

        // Create a company. This will be done elsewhere once procedural company generation is set up.
		transportCompany = Instantiate (companyTemplate).GetComponent<Company>();
		productionCompany = Instantiate (companyTemplate).GetComponent<Company>();

        // Get a random name for the start
		name = _starNames[Random.Range(0, _starNames.Length)];

        // Generate a random number to be the number of planets we have in the system
		int numOfPlanets = Random.Range (planetSpawnMin, planetSpawnMax);

        // Generate planets and moons
		for (int i = 0; i < Random.Range(planetSpawnMin, planetSpawnMax); i++)
			SpawnPlanet (i, numOfPlanets);

        // Spawn some ships
		for (int i = 0; i < numOfShips; i++)
			SpawnShip ();

        // Give the star a sprite
        GetComponent<SpriteRenderer>().sprite = _assets.starSprites["star_blue01"];

		_infoPanel = GameObject.FindGameObjectWithTag ("Infopanel");
	}

	public void Update()
    {
		if (_selected)
			_infoPanel.SendMessage("DisplayInfo", ObjectInfo());
	}

	/*
     * System generation methods
     */

	private void SpawnShip()
    {
        // Get the position of the new ship
        Vector2 pos = new Vector2(Random.Range(0, planetSeperationDistance), Random.Range(0, planetSeperationDistance));
        pos += (Vector2)transform.position;

        // Create the ship from the prefab template and set its position
        GameObject newShip = Instantiate (shipTemplate);
        newShip.transform.position = pos;

		AssetLoader assets = FindObjectOfType<AssetLoader>();

		// Set the ship's data from json
		Ship shipComponent = newShip.GetComponent<Ship>();
		JsonUtility.FromJsonOverwrite(assets.shipData ["namean"], shipComponent);

        // Add it as an employee to the company
        Employee e = newShip.GetComponent<Employee>();
        transportCompany.AddEmployee(e);

        // Set up which contract types this employee can accept
        e.AddAcceptedContractType<FreightContract>();
    }

	private void SpawnPlanet(int planetNumber, int maxPlanets)
    {
        // Get a random position for the planet.
        // Note that the distance from the star is set based on how many planets have been generated already
        // and the planet seperation distance.
		Vector2 pos = Random.insideUnitCircle;
		pos.Normalize ();
		pos *= (planetSeperationDistance * (planetNumber + 1));

        // Create the planet from the prefab template and set its position
		GameObject newPlanet = Instantiate(planetTemplate, transform);
		newPlanet.transform.parent = gameObject.transform;
		newPlanet.transform.Translate (pos);

        // Give the planet a sprite and a name
		GiveSprite (newPlanet);
		newPlanet.name = name + " " + (planetNumber + 1);

		// Create resource, storage and production nodes to be added to the planet
		StorageNode newSNode = Instantiate (storageNodeTemplate, newPlanet.transform).GetComponent<StorageNode>();
		ResourceNode newRNode = Instantiate (resourceNodeTemplates[Random.Range(0, resourceNodeTemplates.Length)], newPlanet.transform).GetComponent<ResourceNode>();
		ProductionNode newPNode = Instantiate (factoryTemplates[Random.Range(0, factoryTemplates.Length)], newPlanet.transform).GetComponent<ProductionNode>();

		// Connect the industry nodes to the storage node.
		newRNode.connectedStorageNode = newSNode;
		newPNode.connectedStorageNode = newSNode;

		// Give the company ownership of the nodes
		productionCompany.AddEmployee(newSNode.gameObject.GetComponent<Employee>());
		productionCompany.AddEmployee(newRNode.gameObject.GetComponent<Employee>());
		productionCompany.AddEmployee(newPNode.gameObject.GetComponent<Employee>());

        // Add the industry nodes to the planet
		Planet planetScript = newPlanet.GetComponent<Planet> () as Planet;
		planetScript.industryNodes.Add (newRNode);
		planetScript.industryNodes.Add (newPNode);
		planetScript.industryNodes.Add (newSNode);

		planets.Add (planetScript);

        // Spawn a moon if our random number is 0
		if (Random.Range (0, 5) == 0)
			SpawnMoon (newPlanet);
	}

	private void GiveSprite(GameObject newPlanet)
    {
		SpriteRenderer renderer = newPlanet.GetComponent<SpriteRenderer> ();
		renderer.sprite = _assets.planetSprites["planet_34"];

		CircleCollider2D collider = newPlanet.GetComponent<CircleCollider2D> ();
		Vector3 spriteHalfSize = renderer.sprite.bounds.extents;
		collider.radius = spriteHalfSize.x > spriteHalfSize.y ? spriteHalfSize.x : spriteHalfSize.y;
	}

	private void SpawnMoon(GameObject parentPlanet)
    {
		Vector2 pos = Random.insideUnitCircle;
		pos.Normalize ();
		pos *= moonDistance;

		GameObject newMoon = Instantiate (planetTemplate, parentPlanet.transform);
		newMoon.transform.parent = parentPlanet.transform;
		newMoon.transform.Translate (pos);

		newMoon.GetComponent<SpriteRenderer>().sprite = _assets.planetSprites["planet_35"];

        newMoon.transform.localScale /= 2;

		newMoon.name = parentPlanet.gameObject.name + " moon";

		// Add resource and production nodes to the new moon.
		StorageNode newSNode = Instantiate (storageNodeTemplate, newMoon.transform).GetComponent<StorageNode>();
		ResourceNode newRNode = Instantiate (resourceNodeTemplates[Random.Range(0, resourceNodeTemplates.Length)], newMoon.transform).GetComponent<ResourceNode>();
		ProductionNode newPNode = Instantiate (factoryTemplates[Random.Range(0, factoryTemplates.Length)], newMoon.transform).GetComponent<ProductionNode>();

		// Connect the industry nodes to the storage node.
		newRNode.connectedStorageNode = newSNode;
		newPNode.connectedStorageNode = newSNode;

		// Give the company ownership of the nodes
		productionCompany.AddEmployee(newSNode.gameObject.GetComponent<Employee>());
		productionCompany.AddEmployee(newRNode.gameObject.GetComponent<Employee>());
		productionCompany.AddEmployee(newPNode.gameObject.GetComponent<Employee>());

		Planet planetScript = newMoon.GetComponent<Planet> () as Planet;
		planetScript.industryNodes.Add (newRNode);
		planetScript.industryNodes.Add (newPNode);
		planetScript.industryNodes.Add (newSNode);

        planets.Add (newMoon.GetComponent<Planet>());
	}

    /*
     * System information lookup methods
     */

    // Returns a list of all unique resource types with at least one unit currently in storage in the system.
    public List<string> ResourceTypesAvailable()
    {
        List<string> result = new List<string>();

        foreach (Planet p in planets)
            result.AddRange(p.ResourceTypesAvailable());

        return result;
    }

    // Returns a list of all unique resource types produced in the system.
    public List<string> ResourceTypesProduced()
    {
        List<string> result = new List<string>();

        foreach (Planet p in planets)
            result.AddRange(ResourceTypesProduced());

        return result;
    }

    // Returns a list of all resource objects stored in the system.
    public List<Resource> ResourcesAvailable()
    {
        List<Resource> result = new List<Resource>();

        result = planets.SelectMany(planet => planet.StoredResources()).ToList();

        return result;
    }

    public bool HasResourceAmount(Resource resource)
    {
        return ResourcesAvailable().Exists(r => r.type == resource.type && r.amount >= resource.amount);
    }

    // Returns a dictionary of resource objects mapped to their storage locations
    public Dictionary<Resource, StorageNode> ResourceLocations()
    {
        Dictionary<Resource, StorageNode> result = new Dictionary<Resource, StorageNode>();

        foreach (Planet p in planets)
            foreach (KeyValuePair<Resource, StorageNode> kvp in p.ResourceLocations())
                result.Add(kvp.Key, kvp.Value);

        return result;
    }

    /*
     * Event listeners
     */

    public void OnMouseOver()
    {
		_selected = true;
		FindObjectOfType<InputController> ().inspectableSelected = true;
	}

	public void OnMouseExit()
    {
		_selected = false;
		FindObjectOfType<InputController> ().inspectableSelected = false;
	}

	public string ObjectInfo ()
    {
		string result = "Name: " + gameObject.name + "\nAccepted contracts in system: ";
        result += transportCompany.acceptedContracts.Count;

        result += "\nOutstanding contracts in system: ";
        result += transportCompany.outstandingContracts.Count;

		return result;
	}
}