// Sekme yerleşimi — patron için görünümler: Panel / Satışlar / Cari / Soğuk Zincir / AI.
import { Tabs } from 'expo-router';

export default function TabsLayout() {
  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: '#15803d',
        headerStyle: { backgroundColor: '#166534' },
        headerTintColor: '#fff',
      }}
    >
      <Tabs.Screen name="index" options={{ title: 'Panel' }} />
      <Tabs.Screen name="satislar" options={{ title: 'Satışlar' }} />
      <Tabs.Screen name="cari" options={{ title: 'Cari' }} />
      <Tabs.Screen name="soguk-zincir" options={{ title: 'Soğuk Zincir' }} />
      <Tabs.Screen name="ai" options={{ title: 'AI' }} />
    </Tabs>
  );
}
