using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

//[RequireComponent(typeof(AudioMixer))]
public static class UtilsAudio {

	public static void PlayAtPosition(this AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f, AudioMixerGroup audioMixerGroup = null)
	{
		var tmpObject = new GameObject("TempAudio"); // create the temp object
		tmpObject.transform.position = position; // set its position
		var audioSource = tmpObject.AddComponent<AudioSource>(); // add an audio source
		audioSource.clip = clip; // define the clip
		audioSource.outputAudioMixerGroup = audioMixerGroup; // set the desired AudioMixerGroup. If not specified, plays without one
		audioSource.volume = volume;
		audioSource.spatialBlend = spatialBlend; //always play 3D sound

		// set other audioSource properties here, if desired
		audioSource.Play(); // start the clip
		MonoBehaviorUtility.DestroyGameObject(tmpObject, clip.length); // destroy object after clip duration
//		return audioSource; // return the AudioSource reference
	}

    public static void Play2D(this AudioClip clip, float volume = 1, AudioMixerGroup audioMixerGroup = null, Vector3 position = default(Vector3))
    {
        clip.PlayAtPosition(position, volume, 0, audioMixerGroup);
    }

//    TODO consider placing this in a file of its own, if needed often in other files.
    private class MonoBehaviorUtility : MonoBehaviour
    {
        public static void DestroyGameObject(GameObject gameObject, float time)
        {
            Destroy(gameObject, time);
        }
    }
}
