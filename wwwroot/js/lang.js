function setCultureCookie(culture) {
    const value = `c=${culture}|uic=${culture}`;
    document.cookie = `.AspNetCore.Culture=${value}; path=/; expires=Fri, 31 Dec 9999 23:59:59 GMT`;
}
