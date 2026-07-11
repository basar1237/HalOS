// Expo varsayılan Metro yapılandırması. expo-router'ın giriş noktası (expo-router/entry)
// çözümlemesi için gereklidir (SDK 54+).
const { getDefaultConfig } = require('expo/metro-config');

const config = getDefaultConfig(__dirname);

module.exports = config;
