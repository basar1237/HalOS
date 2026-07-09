// Giriş noktası — panele yönlendir; oturum yoksa kök yerleşim login'e düşürür.
import { Redirect } from 'expo-router';

export default function Index() {
  return <Redirect href="/(tabs)" />;
}
