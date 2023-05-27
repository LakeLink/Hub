import { withIronSessionApiRoute } from "iron-session/next";
import { NextApiRequest, NextApiResponse } from "next";
import { casV1Users } from "~/lib/cas";
import { sessionOptions } from "~/lib/session";

import { User } from "~/lib/user";

async function handler(request: NextApiRequest, response: NextApiResponse) {
  try {
    const userInfo = await casV1Users(request.body.username, request.body.password)
    request.session.verified = true;
    request.session.org = userInfo.organization;
    request.session.casId = request.body.username;
    request.session.casPassword = request.body.password;
    await request.session.save();

    response.redirect(302, '/')
  } catch (error) {
    response.status(500).json({ message: (error as Error).message });
  }
}

export default withIronSessionApiRoute(handler, (req, _) => {
  let ttl = req.body.rememberme ? 0 : 60*5;
  return {
    ttl,
    ...sessionOptions
  }
});
